namespace BuildingBlocks.Abstractions.Connectors;

/// <summary>
/// Schema cấu hình cho một connector. Đây là trung tâm của config-driven system.
/// Mỗi third-party integration được mô tả hoàn toàn bằng config này.
/// </summary>
public class ConnectorConfig
{
    /// <summary>Unique ID</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Tên hiển thị (vd: "VietcomBank Transfer API")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Loại connector: "generic-http" hoặc tên DLL connector</summary>
    public string ConnectorType { get; set; } = "generic-http";

    /// <summary>Phiên bản config (cho versioning & rollback)</summary>
    public int Version { get; set; } = 1;

    /// <summary>Trạng thái: Active, Inactive, Canary</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Cấu hình endpoint</summary>
    public EndpointConfig Endpoint { get; set; } = new();

    /// <summary>Cấu hình authentication</summary>
    public AuthConfig Authentication { get; set; } = new();

    /// <summary>Danh sách operations được hỗ trợ</summary>
    public List<OperationConfig> Operations { get; set; } = new();

    /// <summary>Cấu hình retry</summary>
    public RetryConfig Retry { get; set; } = new();

    /// <summary>Cấu hình circuit breaker</summary>
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();

    /// <summary>Timeout tổng (ms)</summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>Metadata bổ sung</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EndpointConfig
{
    /// <summary>Base URL của third-party API</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Headers mặc định gửi kèm mọi request</summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}

public class AuthConfig
{
    /// <summary>Loại auth: None, ApiKey, BasicAuth, BearerToken, OAuth2, Custom</summary>
    public string Type { get; set; } = "None";

    /// <summary>Tham số auth (key, secret, token URL...)</summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class OperationConfig
{
    /// <summary>Tên operation (vd: "transfer", "inquiry")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HTTP Method: GET, POST, PUT, DELETE</summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>Path tương đối (vd: "/api/v1/transfer")</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Content-Type</summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>Mapping request: source field → target field</summary>
    public List<FieldMapping> RequestMappings { get; set; } = new();

    /// <summary>Mapping response: source field → target field</summary>
    public List<FieldMapping> ResponseMappings { get; set; } = new();

    /// <summary>Validation rules cho request</summary>
    public List<ValidationRule> Validations { get; set; } = new();
}

public class FieldMapping
{
    /// <summary>Field nguồn (hỗ trợ dot notation: "data.account.number")</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Field đích</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Giá trị mặc định nếu source null</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Transform expression (vd: "ToUpper", "Format:yyyy-MM-dd")</summary>
    public string? Transform { get; set; }

    /// <summary>Bắt buộc hay không</summary>
    public bool Required { get; set; }
}

public class ValidationRule
{
    public string Field { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty; // Required, MaxLength:50, Regex:^[0-9]+$
    public string? ErrorMessage { get; set; }
}

public class RetryConfig
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public string BackoffStrategy { get; set; } = "Exponential"; // Fixed, Linear, Exponential
    public List<int> RetryOnStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };
}

public class CircuitBreakerConfig
{
    public int FailureThreshold { get; set; } = 5;
    public int DurationOfBreakSeconds { get; set; } = 30;
    public int SamplingDurationSeconds { get; set; } = 60;
}
