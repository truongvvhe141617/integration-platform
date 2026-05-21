namespace ConfigService.Domain.Entities;

/// <summary>
/// Domain Entity — Integration Config.
/// Không phụ thuộc bất kỳ framework nào.
/// </summary>
public class IntegrationConfig
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string ConfigKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ConnectorType { get; private set; } = "generic-http";
    public string Status { get; private set; } = ConfigStatus.Draft;
    public int Version { get; private set; } = 1;
    public string? Tags { get; private set; }

    // Endpoint
    public string BaseUrl { get; private set; } = string.Empty;
    public string? DefaultHeaders { get; private set; }

    // Auth
    public string AuthType { get; private set; } = "none";
    public string? AuthParams { get; private set; }

    // Resilience
    public int TimeoutMs { get; private set; } = 30000;
    public string? RetryConfig { get; private set; }
    public string? CircuitBreaker { get; private set; }
    public string? Metadata { get; private set; }

    // Audit
    public string CreatedBy { get; private set; } = string.Empty;
    public string? UpdatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    // Navigation
    public IReadOnlyCollection<IntegrationOperation> Operations => _operations.AsReadOnly();
    private readonly List<IntegrationOperation> _operations = new();

    private IntegrationConfig() { } // EF Core

    public static IntegrationConfig Create(
        string tenantId, string configKey, string name, string baseUrl,
        string connectorType, string createdBy)
    {
        return new IntegrationConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigKey = configKey,
            Name = name,
            BaseUrl = baseUrl,
            ConnectorType = connectorType,
            Status = ConfigStatus.Draft,
            Version = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string baseUrl, string? description,
        string authType, string? authParams, string? defaultHeaders,
        int timeoutMs, string? retryConfig, string? circuitBreaker,
        string? tags, string? metadata, string updatedBy)
    {
        Name = name;
        BaseUrl = baseUrl;
        Description = description;
        AuthType = authType;
        AuthParams = authParams;
        DefaultHeaders = defaultHeaders;
        TimeoutMs = timeoutMs;
        RetryConfig = retryConfig;
        CircuitBreaker = circuitBreaker;
        Tags = tags;
        Metadata = metadata;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Activate(string updatedBy)
    {
        if (Status == ConfigStatus.Deleted)
            throw new InvalidOperationException("Cannot activate deleted config");
        Status = ConfigStatus.Active;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(string updatedBy)
    {
        Status = ConfigStatus.Inactive;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        Status = ConfigStatus.Deleted;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void AddOperation(IntegrationOperation operation)
    {
        _operations.Add(operation);
    }

    public void ClearOperations()
    {
        _operations.Clear();
    }
}

public static class ConfigStatus
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Inactive = "inactive";
    public const string Archived = "archived";
    public const string Deleted = "deleted";

    public static bool CanTransitionTo(string from, string to) => (from, to) switch
    {
        (Draft, Active) => true,
        (Draft, Archived) => true,
        (Active, Inactive) => true,
        (Active, Archived) => true,
        (Inactive, Active) => true,
        (Inactive, Archived) => true,
        _ => false
    };
}
