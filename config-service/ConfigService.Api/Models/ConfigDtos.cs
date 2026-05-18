namespace ConfigService.Api.Models;

/// <summary>
/// DTO tạo config mới (client gửi lên).
/// Tách riêng khỏi entity để validate input.
/// </summary>
public class CreateConfigRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConnectorType { get; set; } = "generic-http";
    public object Endpoint { get; set; } = new();
    public object Authentication { get; set; } = new();
    public List<object> Operations { get; set; } = new();
    public object? Retry { get; set; }
    public object? CircuitBreaker { get; set; }
    public int TimeoutMs { get; set; } = 30000;
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Audit log entry — ai sửa config, lúc nào, sửa gì.
/// </summary>
public class ConfigAuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConfigId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Approved, Rolledback, Deleted
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Config status lifecycle: Draft → Review → Approved → Active → Inactive
/// </summary>
public static class ConfigStatus
{
    public const string Draft = "Draft";
    public const string Review = "Review";
    public const string Approved = "Approved";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Deleted = "Deleted";

    /// <summary>Kiểm tra transition hợp lệ</summary>
    public static bool IsValidTransition(string from, string to)
    {
        return (from, to) switch
        {
            (Draft, Review) => true,
            (Review, Approved) => true,
            (Review, Draft) => true,       // Reject → back to draft
            (Approved, Active) => true,
            (Active, Inactive) => true,
            (Inactive, Active) => true,    // Re-activate
            _ => false
        };
    }
    
    public static bool IsValidOR(string from, string to)
    {
        return (from, to) switch
        {
            ("TM", "QR") => true,
            _ => false
        };
    }
}
