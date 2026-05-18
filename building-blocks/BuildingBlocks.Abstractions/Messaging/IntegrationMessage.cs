namespace BuildingBlocks.Abstractions.Messaging;

/// <summary>
/// Message gửi qua RabbitMQ cho async integration.
/// App A publish message → Integration Service consume → gọi third-party → publish result.
/// </summary>
public class IntegrationMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string ConnectorConfigId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, string>? Headers { get; set; }
    public string? IdempotencyKey { get; set; }

    /// <summary>Queue name để gửi kết quả callback (optional)</summary>
    public string? ReplyTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Kết quả integration gửi qua RabbitMQ (callback).
/// </summary>
public class IntegrationResultMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
    public double ExecutionTimeMs { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
