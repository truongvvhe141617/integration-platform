using ConfigService.Domain.Entities;

namespace ConfigService.Application.Common.Models;

public class IntegrationConfigDto
{
    public Guid Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConnectorType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Tags { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string? DefaultHeaders { get; set; }
    public string AuthType { get; set; } = string.Empty;
    public int TimeoutMs { get; set; }
    public string? RetryConfig { get; set; }
    public string? CircuitBreaker { get; set; }
    public string? Metadata { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OperationDto> Operations { get; set; } = new();

    public static IntegrationConfigDto FromEntity(IntegrationConfig entity)
    {
        return new IntegrationConfigDto
        {
            Id = entity.Id,
            ConfigKey = entity.ConfigKey,
            Name = entity.Name,
            Description = entity.Description,
            ConnectorType = entity.ConnectorType,
            Status = entity.Status,
            Version = entity.Version,
            Tags = entity.Tags,
            BaseUrl = entity.BaseUrl,
            DefaultHeaders = entity.DefaultHeaders,
            AuthType = entity.AuthType,
            TimeoutMs = entity.TimeoutMs,
            RetryConfig = entity.RetryConfig,
            CircuitBreaker = entity.CircuitBreaker,
            Metadata = entity.Metadata,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Operations = entity.Operations.Select(OperationDto.FromEntity).ToList()
        };
    }
}

public class OperationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public List<MappingDto> RequestMappings { get; set; } = new();
    public List<MappingDto> ResponseMappings { get; set; } = new();
    public List<ValidationDto> Validations { get; set; } = new();

    public static OperationDto FromEntity(IntegrationOperation op) => new()
    {
        Id = op.Id, Name = op.Name, HttpMethod = op.HttpMethod,
        Path = op.Path, ContentType = op.ContentType,
        Description = op.Description, IsEnabled = op.IsEnabled,
        RequestMappings = op.RequestMappings.Select(m => new MappingDto
        {
            SourceField = m.SourceField, TargetField = m.TargetField,
            DefaultValue = m.DefaultValue, Transform = m.Transform,
            IsRequired = m.IsRequired, SortOrder = m.SortOrder
        }).ToList(),
        ResponseMappings = op.ResponseMappings.Select(m => new MappingDto
        {
            SourceField = m.SourceField, TargetField = m.TargetField,
            DefaultValue = m.DefaultValue, Transform = m.Transform,
            SortOrder = m.SortOrder
        }).ToList(),
        Validations = op.ValidationRules.Select(v => new ValidationDto
        {
            Field = v.Field, Rule = v.Rule,
            ErrorMessage = v.ErrorMessage, SortOrder = v.SortOrder
        }).ToList()
    };
}

public class MappingDto
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Transform { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public class ValidationDto
{
    public string Field { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int SortOrder { get; set; }
}
