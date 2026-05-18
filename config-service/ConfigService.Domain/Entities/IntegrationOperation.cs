namespace ConfigService.Domain.Entities;

public class IntegrationOperation
{
    public Guid Id { get; private set; }
    public Guid ConfigId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = "POST";
    public string Path { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/json";
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<RequestMapping> RequestMappings => _requestMappings.AsReadOnly();
    public IReadOnlyCollection<ResponseMapping> ResponseMappings => _responseMappings.AsReadOnly();
    public IReadOnlyCollection<ValidationRule> ValidationRules => _validationRules.AsReadOnly();

    private readonly List<RequestMapping> _requestMappings = new();
    private readonly List<ResponseMapping> _responseMappings = new();
    private readonly List<ValidationRule> _validationRules = new();

    private IntegrationOperation() { }

    public static IntegrationOperation Create(Guid configId, string name,
        string httpMethod, string path, string? contentType = null, string? description = null)
    {
        return new IntegrationOperation
        {
            Id = Guid.NewGuid(),
            ConfigId = configId,
            Name = name,
            HttpMethod = httpMethod.ToUpperInvariant(),
            Path = path,
            ContentType = contentType ?? "application/json",
            Description = description
        };
    }

    public void AddRequestMapping(string source, string target, string? defaultValue,
        string? transform, bool isRequired, int sortOrder = 0)
    {
        _requestMappings.Add(new RequestMapping
        {
            Id = Guid.NewGuid(), OperationId = Id,
            SourceField = source, TargetField = target,
            DefaultValue = defaultValue, Transform = transform,
            IsRequired = isRequired, SortOrder = sortOrder
        });
    }

    public void AddResponseMapping(string source, string target,
        string? defaultValue, string? transform, int sortOrder = 0)
    {
        _responseMappings.Add(new ResponseMapping
        {
            Id = Guid.NewGuid(), OperationId = Id,
            SourceField = source, TargetField = target,
            DefaultValue = defaultValue, Transform = transform,
            SortOrder = sortOrder
        });
    }

    public void AddValidationRule(string field, string rule, string? errorMessage, int sortOrder = 0)
    {
        _validationRules.Add(new ValidationRule
        {
            Id = Guid.NewGuid(), OperationId = Id,
            Field = field, Rule = rule,
            ErrorMessage = errorMessage, SortOrder = sortOrder
        });
    }
}

public class RequestMapping
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Transform { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public class ResponseMapping
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Transform { get; set; }
    public int SortOrder { get; set; }
}

public class ValidationRule
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int SortOrder { get; set; }
}
