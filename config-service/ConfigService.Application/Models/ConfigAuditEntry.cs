namespace ConfigService.Application.Models;

/// <summary>
/// Audit log entry — ai sửa config, lúc nào, sửa gì.
/// </summary>
public class ConfigAuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConfigId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty;
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

    public static bool IsValidTransition(string from, string to)
    {
        return (from, to) switch
        {
            (Draft, Review) => true,
            (Review, Approved) => true,
            (Review, Draft) => true,
            (Approved, Active) => true,
            (Active, Inactive) => true,
            (Inactive, Active) => true,
            _ => false
        };
    }
}
