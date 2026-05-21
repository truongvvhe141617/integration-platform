namespace BuildingBlocks.Abstractions.Messaging;

/// <summary>
/// Message được publish lên RabbitMQ khi Integration Service nhận webhook từ đối tác.
/// Exchange: webhook.inbound
/// Routing key: webhook.{connectorConfigId}.{operation}
/// App nội bộ subscribe exchange này để nhận callback từ đối tác.
/// </summary>
public class InboundWebhookMessage
{
    /// <summary>ID định danh message (duy nhất)</summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>ID để trace request end-to-end</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Config ID xác định đây là webhook từ đối tác nào</summary>
    public string ConnectorConfigId { get; set; } = string.Empty;

    /// <summary>Tên operation (vd: "payment-callback", "transfer-notification")</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Dữ liệu đã được map từ format đối tác → format chuẩn</summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>Headers gốc từ request của đối tác</summary>
    public Dictionary<string, string> OriginalHeaders { get; set; } = new();

    /// <summary>IP của đối tác gọi vào</summary>
    public string? SourceIp { get; set; }

    /// <summary>Kết quả xác thực chữ ký webhook (HMAC/signature)</summary>
    public bool SignatureVerified { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
