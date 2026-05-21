namespace IntegrationService.Application.Models;

public class IntegrationRequest
{
    public string ConnectorConfigId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, string>? Headers { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class AsyncIntegrationRequest
{
    public string ConnectorConfigId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, string>? Headers { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Response chuẩn hóa.
/// Thành công: success=true, data có giá trị, error fields null
/// Thất bại: success=false, error fields có giá trị, httpStatus map theo errorCode
/// </summary>
public class IntegrationResponse
{
    public bool Success { get; set; }
    public int HttpStatus { get; set; }
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>Mã lỗi chuẩn: INT_CFG_001, CON_NET_001...</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Message đã dịch theo ngôn ngữ (Accept-Language)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Chi tiết lỗi kỹ thuật (chỉ hiện ở dev, ẩn ở production)</summary>
    public string? ErrorDetail { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public double ExecutionTimeMs { get; set; }
}
