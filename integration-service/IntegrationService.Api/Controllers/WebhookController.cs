using System.Text.Json;
using IntegrationService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

/// <summary>
/// Flow 2: Nhận callback/webhook từ đối tác bên ngoài.
///
/// Endpoint: POST /api/v1/webhook/{configId}/{operation}
///
/// Ví dụ:
///   POST /api/v1/webhook/ewallet-momo/payment-callback
///   POST /api/v1/webhook/bank-b-transfer/transfer-notification
///   POST /api/v1/webhook/kyc-service/verification-result
///
/// Sau khi nhận → map payload → publish lên RabbitMQ exchange "webhook.inbound"
/// App nội bộ subscribe exchange này để nhận và xử lý.
/// </summary>
[ApiController]
[Route("api/v1/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookProcessor _webhookProcessor;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookProcessor webhookProcessor, ILogger<WebhookController> logger)
    {
        _webhookProcessor = webhookProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Nhận webhook/callback từ đối tác bên ngoài.
    /// Không yêu cầu JWT — đối tác gọi vào không có JWT của hệ thống.
    /// Xác thực bằng webhook signature (HMAC) nếu config có WebhookSecret.
    /// </summary>
    [HttpPost("{configId}/{operation}")]
    public async Task<IActionResult> ReceiveWebhook(
        [FromRoute] string configId,
        [FromRoute] string operation,
        CancellationToken cancellationToken)
    {
        // Đọc raw body để tránh mất dữ liệu khi parse
        string rawBody;
        using (var reader = new System.IO.StreamReader(Request.Body))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        Dictionary<string, object> payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            // Một số đối tác gửi form-urlencoded hoặc text
            payload = new Dictionary<string, object> { ["raw"] = rawBody };
        }

        // Lấy headers liên quan (signature, timestamp...)
        var headers = Request.Headers
            .Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)
                     || h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Request.Headers["X-Request-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        Response.Headers["X-Correlation-Id"] = correlationId;

        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation(
            "Webhook received: ConfigId={ConfigId} Op={Op} CorrId={CorrId} SourceIp={Ip}",
            configId, operation, correlationId, sourceIp);

        var result = await _webhookProcessor.ProcessAsync(
            configId, operation, payload, headers, sourceIp, correlationId, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Webhook processing failed: ConfigId={ConfigId} Op={Op} Error={Error}",
                configId, operation, result.Error);
            return StatusCode(result.StatusCode, new { success = false, error = result.Error, correlationId });
        }

        // Trả 200 ngay — đối tác không cần chờ app nội bộ xử lý xong
        return Ok(new
        {
            success = true,
            correlationId = result.CorrelationId,
            messageId = result.MessageId,
            status = "RECEIVED"
        });
    }
}
