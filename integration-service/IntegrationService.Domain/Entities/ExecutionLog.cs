using IntegrationService.Domain.Common;

namespace IntegrationService.Domain.Entities;

/// <summary>
/// Lưu lịch sử mỗi lần gọi integration.
/// DB riêng của Integration Service.
/// </summary>
public class ExecutionLog : EntityBase
{
    public string TenantId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;

    // Request
    public string? RequestBody { get; set; }
    public string? RequestHeaders { get; set; }

    // Response
    public string? ResponseBody { get; set; }
    public int? ResponseStatus { get; set; }

    // Timing
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }

    // Result
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    // Retry
    public int RetryCount { get; set; }

    // Source
    public string? CallerIp { get; set; }
    public string? CallerService { get; set; }

    // Navigation
    public ICollection<RetryLog> RetryLogs { get; set; } = new List<RetryLog>();
}

public class RetryLog : EntityBase
{
    public Guid ExecutionLogId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DelayMs { get; set; }

    public ExecutionLog? ExecutionLog { get; set; }
}
