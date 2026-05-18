using System.Diagnostics;
using BuildingBlocks.Abstractions.Connectors;
using BuildingBlocks.Abstractions.ErrorCodes;
using BuildingBlocks.Core.Connectors;
using BuildingBlocks.Core.Localization;
using BuildingBlocks.Core.Mapping;
using IntegrationService.Api.Interfaces;
using IntegrationService.Api.Models;

namespace IntegrationService.Api.Services;

public class IntegrationExecutor : IIntegrationExecutor
{
    private readonly IConnectorFactory _connectorFactory;
    private readonly IMappingEngine _mappingEngine;
    private readonly IConfigProvider _configProvider;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMessageLocalizer _localizer;
    private readonly ILogger<IntegrationExecutor> _logger;

    public IntegrationExecutor(
        IConnectorFactory connectorFactory, IMappingEngine mappingEngine,
        IConfigProvider configProvider, IIdempotencyStore idempotencyStore,
        IMessageLocalizer localizer, ILogger<IntegrationExecutor> logger)
    {
        _connectorFactory = connectorFactory;
        _mappingEngine = mappingEngine;
        _configProvider = configProvider;
        _idempotencyStore = idempotencyStore;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IntegrationResponse> ExecuteAsync(
        IntegrationRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var lang = request.Headers?.GetValueOrDefault("Accept-Language") ?? "en";

        // 1. Idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var cached = await _idempotencyStore.GetAsync(request.IdempotencyKey);
            if (cached != null) return cached;
        }

        // 2. Load config
        var config = await _configProvider.GetConfigAsync(request.ConnectorConfigId);
        if (config == null)
            return Err(ErrorCodes.INT_CFG_001, lang, correlationId, request.ConnectorConfigId);

        if (config.Status != "Active")
            return Err(ErrorCodes.INT_CFG_002, lang, correlationId, config.Id, config.Status);

        // 3. Find operation
        var operation = config.Operations.FirstOrDefault(o => o.Name == request.Operation);
        if (operation == null)
            return Err(ErrorCodes.INT_CFG_003, lang, correlationId, request.Operation, config.Id);

        // 4. Validate
        var errors = _mappingEngine.Validate(operation.Validations, request.Data);
        if (errors.Count > 0)
            return Err(ErrorCodes.INT_VAL_001, lang, correlationId, string.Join("; ", errors));

        // 5. Map request
        var mappedPayload = _mappingEngine.MapRequest(operation.RequestMappings, request.Data);

        // 6. Create connector
        IConnector connector;
        try { connector = _connectorFactory.Create(config); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connector load failed: {Type}", config.ConnectorType);
            return Err(ErrorCodes.INT_SYS_002, lang, correlationId, config.ConnectorType, ex.Message);
        }

        // 7. Execute
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(config.TimeoutMs);

        ConnectorResponse connectorResponse;
        try
        {
            connectorResponse = await connector.ExecuteAsync(new ConnectorRequest
            {
                ConfigId = config.Id, Operation = request.Operation,
                Payload = mappedPayload, Headers = request.Headers ?? new(),
                Config = config, CorrelationId = correlationId
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return Err(ErrorCodes.INT_TMO_001, lang, correlationId, config.TimeoutMs);
        }
        catch (HttpRequestException ex)
        {
            return Err(ErrorCodes.INT_NET_001, lang, correlationId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connector failed: {ConfigId}/{Op}", config.Id, request.Operation);
            return Err(ErrorCodes.CON_SYS_001, lang, correlationId, ex.Message);
        }

        // 8. Map response
        if (connectorResponse.IsSuccess && connectorResponse.Data.Count > 0
            && operation.ResponseMappings.Count > 0)
        {
            var mapped = _mappingEngine.MapResponse(operation.ResponseMappings, connectorResponse.Data);
            connectorResponse.Data = mapped.Count > 0 ? mapped : connectorResponse.Data;
        }

        _logger.LogInformation("Integration: Config={Id} Op={Op} Success={S} Time={T}ms",
            config.Id, request.Operation, connectorResponse.IsSuccess,
            connectorResponse.ExecutionTime.TotalMilliseconds);

        // 9. Build response
        IntegrationResponse response;
        if (connectorResponse.IsSuccess)
        {
            response = new IntegrationResponse
            {
                Success = true,
                HttpStatus = 200,
                Data = connectorResponse.Data,
                CorrelationId = correlationId,
                TraceId = Activity.Current?.TraceId.ToString(),
                ExecutionTimeMs = connectorResponse.ExecutionTime.TotalMilliseconds
            };
        }
        else
        {
            // Map HTTP status từ third-party response
            var errorCode = connectorResponse.StatusCode switch
            {
                400 => ErrorCodes.INT_VAL_001,
                401 or 403 => ErrorCodes.CON_AUTH_001,
                404 => ErrorCodes.INT_CFG_001,
                408 => ErrorCodes.CON_TMO_001,
                429 => ErrorCodes.GW_TMO_001,
                >= 500 and < 600 => ErrorCodes.CON_BIZ_001,
                0 => ErrorCodes.INT_NET_002, // Connection refused
                _ => ErrorCodes.INT_BIZ_001
            };

            response = new IntegrationResponse
            {
                Success = false,
                HttpStatus = ErrorCodes.ToHttpStatus(errorCode),
                ErrorCode = errorCode,
                ErrorMessage = _localizer.Get(errorCode, lang, connectorResponse.StatusCode),
                ErrorDetail = connectorResponse.ErrorMessage,
                CorrelationId = correlationId,
                TraceId = Activity.Current?.TraceId.ToString(),
                ExecutionTimeMs = connectorResponse.ExecutionTime.TotalMilliseconds
            };
        }

        // 10. Idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
            await _idempotencyStore.SetAsync(request.IdempotencyKey, response, TimeSpan.FromHours(24));

        return response;
    }

    public async Task<HealthCheckResult> CheckConnectorHealthAsync(
        string configId, CancellationToken cancellationToken)
    {
        var config = await _configProvider.GetConfigAsync(configId);
        if (config == null)
            return new HealthCheckResult { ConfigId = configId, Healthy = false, Error = "Config not found" };
        try
        {
            var connector = _connectorFactory.Create(config);
            var healthy = await connector.HealthCheckAsync(cancellationToken);
            return new HealthCheckResult { ConfigId = configId, Healthy = healthy };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult { ConfigId = configId, Healthy = false, Error = ex.Message };
        }
    }

    private IntegrationResponse Err(string errorCode, string lang, string correlationId, params object[] args)
    {
        return new IntegrationResponse
        {
            Success = false,
            HttpStatus = ErrorCodes.ToHttpStatus(errorCode),
            ErrorCode = errorCode,
            ErrorMessage = _localizer.Get(errorCode, lang, args),
            CorrelationId = correlationId,
            TraceId = Activity.Current?.TraceId.ToString()
        };
    }
}
