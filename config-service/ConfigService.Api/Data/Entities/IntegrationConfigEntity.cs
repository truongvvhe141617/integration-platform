using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConfigService.Api.Data.Entities;

[Table("integration_config")]
public class IntegrationConfigEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string ConfigId { get; set; } = string.Empty;
    [MaxLength(255)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(50)] public string ConnectorType { get; set; } = "generic-http";
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public int Version { get; set; } = 1;
    [MaxLength(500)] public string BaseUrl { get; set; } = string.Empty;
    public string? DefaultHeaders { get; set; }
    [MaxLength(50)] public string AuthType { get; set; } = "none";
    public string? AuthParams { get; set; }
    public int TimeoutMs { get; set; } = 30000;
    public string? RetryConfig { get; set; }
    public string? CircuitBreakerConfig { get; set; }
    [MaxLength(500)] public string? Tags { get; set; }
    public string? Metadata { get; set; }
    [MaxLength(100)] public string CreatedBy { get; set; } = string.Empty;
    [MaxLength(100)] public string? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public List<IntegrationOperationEntity> Operations { get; set; } = new();
}

[Table("integration_operation")]
public class IntegrationOperationEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConfigId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(10)] public string HttpMethod { get; set; } = "POST";
    [MaxLength(500)] public string Path { get; set; } = string.Empty;
    [MaxLength(100)] public string? ContentType { get; set; } = "application/json";
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    public IntegrationConfigEntity Config { get; set; } = null!;
    public List<RequestMappingEntity> RequestMappings { get; set; } = new();
    public List<ResponseMappingEntity> ResponseMappings { get; set; } = new();
    public List<ValidationRuleEntity> Validations { get; set; } = new();
}

[Table("request_mapping")]
public class RequestMappingEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; }
    [MaxLength(200)] public string SourceField { get; set; } = string.Empty;
    [MaxLength(200)] public string TargetField { get; set; } = string.Empty;
    [MaxLength(500)] public string? DefaultValue { get; set; }
    [MaxLength(100)] public string? Transform { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public IntegrationOperationEntity Operation { get; set; } = null!;
}

[Table("response_mapping")]
public class ResponseMappingEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; }
    [MaxLength(200)] public string SourceField { get; set; } = string.Empty;
    [MaxLength(200)] public string TargetField { get; set; } = string.Empty;
    [MaxLength(500)] public string? DefaultValue { get; set; }
    [MaxLength(100)] public string? Transform { get; set; }
    public int SortOrder { get; set; }
    public IntegrationOperationEntity Operation { get; set; } = null!;
}

[Table("validation_rule")]
public class ValidationRuleEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; }
    [MaxLength(200)] public string Field { get; set; } = string.Empty;
    [MaxLength(200)] public string Rule { get; set; } = string.Empty;
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    public int SortOrder { get; set; }
    public IntegrationOperationEntity Operation { get; set; } = null!;
}

[Table("config_history")]
public class ConfigHistoryEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConfigId { get; set; }
    public int Version { get; set; }
    public string Snapshot { get; set; } = string.Empty;
    [MaxLength(20)] public string ChangeType { get; set; } = string.Empty;
    [MaxLength(100)] public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? ChangeNote { get; set; }
}

[Table("audit_log")]
public class AuditLogEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    [MaxLength(100)] public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
    [MaxLength(50)] public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
