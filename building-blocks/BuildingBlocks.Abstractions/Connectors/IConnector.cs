namespace BuildingBlocks.Abstractions.Connectors;

/// <summary>
/// Interface cốt lõi cho mọi connector.
/// Mọi connector (Generic HTTP hoặc Custom DLL) đều phải implement interface này.
/// </summary>
public interface IConnector
{
    /// <summary>
    /// Unique identifier của connector type (vd: "generic-http", "bank-abc", "wallet-xyz")
    /// </summary>
    string ConnectorType { get; }

    /// <summary>
    /// Thực thi integration call tới third-party system.
    /// </summary>
    /// <param name="context">Chứa toàn bộ thông tin cần thiết: config, request data, headers...</param>
    /// <param name="cancellationToken">Cancellation token cho timeout control</param>
    /// <returns>Kết quả từ third-party system</returns>
    Task<ConnectorResponse> ExecuteAsync(ConnectorRequest context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra kết nối tới third-party system (health check).
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Request context chứa toàn bộ thông tin để connector thực thi.
/// </summary>
public class ConnectorRequest
{
    /// <summary>ID cấu hình connector đang sử dụng</summary>
    public string ConfigId { get; set; } = string.Empty;

    /// <summary>Operation cần thực hiện (vd: "transfer", "inquiry", "payment")</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Dữ liệu request đã được map theo config</summary>
    public Dictionary<string, object> Payload { get; set; } = new();

    /// <summary>Headers bổ sung</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Cấu hình connector (endpoint, auth, mapping...)</summary>
    public ConnectorConfig? Config { get; set; }

    /// <summary>Correlation ID cho distributed tracing</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Response từ connector sau khi gọi third-party.
/// </summary>
public class ConnectorResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}
