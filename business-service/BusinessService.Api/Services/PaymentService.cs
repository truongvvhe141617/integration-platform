using System.Text;
using System.Text.Json;
using BusinessService.Api.Interfaces;
using BusinessService.Api.Models;

namespace BusinessService.Api.Services;

/// <summary>
/// Payment service — business logic tách khỏi controller.
/// Gọi Integration Service để tích hợp với third-party payment providers.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly HttpClient _integrationClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IHttpClientFactory httpClientFactory, ILogger<PaymentService> logger)
    {
        _integrationClient = httpClientFactory.CreateClient("IntegrationService");
        _logger = logger;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(
        PaymentRequest request, CancellationToken cancellationToken)
    {
        // 1. Business validation
        if (request.Amount <= 0)
            return new PaymentResponse { Success = false, Error = "Amount must be positive" };

        // 2. Determine connector config
        var connectorConfigId = ResolveConnectorConfig(request);

        // 3. Build integration request
        var integrationRequest = new
        {
            connectorConfigId,
            operation = "payment",
            idempotencyKey = request.IdempotencyKey,
            data = new Dictionary<string, object>
            {
                ["orderId"] = request.OrderId,
                ["amount"] = request.Amount,
                ["currency"] = request.Currency ?? "VND",
                ["customer"] = new Dictionary<string, object>
                {
                    ["phone"] = request.CustomerPhone ?? ""
                },
                ["description"] = request.Description ?? $"Payment for order {request.OrderId}"
            }
        };

        // 4. Call Integration Service
        var json = JsonSerializer.Serialize(integrationRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _integrationClient.PostAsync(
            "/api/v1/integration/execute", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Payment integration failed: {Response}", responseBody);
            return new PaymentResponse { Success = false, Error = "Payment processing failed" };
        }

        // 5. Parse response
        var integrationResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var correlationId = integrationResponse.TryGetProperty("correlationId", out var cid)
            ? cid.GetString() ?? "" : "";

        _logger.LogInformation("Payment processed: OrderId={OrderId}, CorrelationId={CorrId}",
            request.OrderId, correlationId);

        return new PaymentResponse
        {
            Success = true,
            CorrelationId = correlationId,
            TransactionId = integrationResponse.TryGetProperty("data", out var data)
                && data.TryGetProperty("transactionId", out var tid)
                ? tid.GetString() : null,
            RedirectUrl = data.TryGetProperty("redirectUrl", out var url)
                ? url.GetString() : null
        };
    }

    private static string ResolveConnectorConfig(PaymentRequest request)
    {
        return request.PaymentMethod.ToLowerInvariant() switch
        {
            "momo" => "ewallet-momo",
            "zalopay" => "ewallet-zalopay",
            "bank-transfer" => $"bank-{request.BankCode?.ToLowerInvariant()}-transfer",
            _ => throw new ArgumentException($"Unsupported payment method: {request.PaymentMethod}")
        };
    }
}
