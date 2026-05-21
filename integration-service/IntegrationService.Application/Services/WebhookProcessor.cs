using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Core.Mapping;
using BuildingBlocks.Core.Messaging;
using IntegrationService.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Application.Services;

/// <summary>
/// Xử lý Flow 2: Đối tác gọi vào → Integration Platform → App nội bộ.
///
/// Flow:
///   1. WebhookController nhận HTTP POST từ đối tác
///   2. WebhookProcessor xác thực chữ ký (nếu config yêu cầu)
///   3. Map payload từ format đối tác → format chuẩn (dùng ResponseMappings của operation)
///   4. Publish InboundWebhookMessage lên exchange "webhook.inbound"
///   5. App nội bộ (Business Service, Worker...) subscribe exchange này → nhận callback
/// </summary>
public interface IWebhookProcessor
{
    Task<WebhookProcessResult> ProcessAsync(
        string connectorConfigId,
        string operation,
        Dictionary<string, object> rawPayload,
        Dictionary<string, string> headers,
        string? sourceIp,
        string correlationId,
        CancellationToken cancellationToken = default);
}

public class WebhookProcessor : IWebhookProcessor
{
    private readonly IConfigProvider _configProvider;
    private readonly IMappingEngine _mappingEngine;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<WebhookProcessor> _logger;

    private const string WebhookExchange = "webhook.inbound";

    public WebhookProcessor(
        IConfigProvider configProvider,
        IMappingEngine mappingEngine,
        IMessagePublisher publisher,
        ILogger<WebhookProcessor> logger)
    {
        _configProvider = configProvider;
        _mappingEngine = mappingEngine;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<WebhookProcessResult> ProcessAsync(
        string connectorConfigId,
        string operation,
        Dictionary<string, object> rawPayload,
        Dictionary<string, string> headers,
        string? sourceIp,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load connector config
        var config = await _configProvider.GetConfigAsync(connectorConfigId);
        if (config == null)
        {
            _logger.LogWarning("Webhook: config not found {ConfigId}", connectorConfigId);
            return WebhookProcessResult.Fail($"Connector config '{connectorConfigId}' not found");
        }

        if (!string.Equals(config.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return WebhookProcessResult.Fail($"Connector '{connectorConfigId}' is not active");
        }

        // 2. Find operation config
        var opConfig = config.Operations.FirstOrDefault(o =>
            string.Equals(o.Name, operation, StringComparison.OrdinalIgnoreCase));

        if (opConfig == null)
        {
            _logger.LogWarning("Webhook: operation '{Op}' not found in config '{ConfigId}'",
                operation, connectorConfigId);
            return WebhookProcessResult.Fail($"Operation '{operation}' not found in config '{connectorConfigId}'");
        }

        // 3. Verify webhook signature (nếu config có HMAC/signature key)
        var signatureVerified = VerifySignature(config, headers, rawPayload);

        if (!signatureVerified && IsSignatureRequired(config))
        {
            _logger.LogWarning("Webhook signature verification failed: {ConfigId}/{Op}", connectorConfigId, operation);
            return WebhookProcessResult.Fail("Webhook signature verification failed", 401);
        }

        // 4. Map payload từ format đối tác → format chuẩn
        // Dùng ResponseMappings vì đây là chiều ngược: đối tác → chúng ta
        Dictionary<string, object> mappedData;
        try
        {
            mappedData = opConfig.ResponseMappings.Count > 0
                ? _mappingEngine.MapResponse(opConfig.ResponseMappings, rawPayload)
                : rawPayload; // Nếu không có mapping config thì giữ nguyên
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook mapping failed: {ConfigId}/{Op}", connectorConfigId, operation);
            return WebhookProcessResult.Fail($"Payload mapping failed: {ex.Message}");
        }

        // 5. Build và publish message lên RabbitMQ
        var message = new InboundWebhookMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            ConnectorConfigId = connectorConfigId,
            Operation = operation,
            Data = mappedData,
            OriginalHeaders = headers,
            SourceIp = sourceIp,
            SignatureVerified = signatureVerified,
            ReceivedAt = DateTime.UtcNow
        };

        var routingKey = $"webhook.{connectorConfigId}.{operation}";
        await _publisher.PublishAsync(WebhookExchange, routingKey, message);

        _logger.LogInformation(
            "Webhook received and queued: Config={ConfigId} Op={Op} CorrId={CorrId} Verified={Verified}",
            connectorConfigId, operation, correlationId, signatureVerified);

        return WebhookProcessResult.Ok(correlationId, message.MessageId);
    }

    private bool VerifySignature(
        BuildingBlocks.Abstractions.Connectors.ConnectorConfig config,
        Dictionary<string, string> headers,
        Dictionary<string, object> payload)
    {
        // Lấy secret key từ authentication config
        if (!config.Authentication.Parameters.TryGetValue("WebhookSecret", out var secret))
            return true; // Không cấu hình signature → bỏ qua

        // Lấy signature từ header (thường là X-Signature hoặc X-Hub-Signature)
        var signatureHeader = config.Authentication.Parameters.TryGetValue("SignatureHeader", out var hdr)
            ? hdr : "X-Signature";

        if (!headers.TryGetValue(signatureHeader, out var providedSignature))
            return false;

        // Tính HMAC-SHA256 của body
        var bodyJson = System.Text.Json.JsonSerializer.Serialize(payload);
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(
            hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(bodyJson))).ToLowerInvariant();

        return string.Equals(computed, providedSignature.Replace("sha256=", ""),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSignatureRequired(
        BuildingBlocks.Abstractions.Connectors.ConnectorConfig config)
    {
        return config.Authentication.Parameters.ContainsKey("WebhookSecret");
    }
}

public class WebhookProcessResult
{
    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; } = 200;
    public string? CorrelationId { get; private set; }
    public string? MessageId { get; private set; }

    public static WebhookProcessResult Ok(string correlationId, string messageId) =>
        new() { Success = true, CorrelationId = correlationId, MessageId = messageId };

    public static WebhookProcessResult Fail(string error, int statusCode = 400) =>
        new() { Success = false, Error = error, StatusCode = statusCode };
}
