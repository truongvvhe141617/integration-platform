using IntegrationService.Api.Models;

namespace IntegrationService.Api.Interfaces;

/// <summary>
/// Service layer xử lý toàn bộ logic integration.
/// Controller chỉ nhận request → gọi executor → trả response.
/// </summary>
public interface IIntegrationExecutor
{
    Task<IntegrationResponse> ExecuteAsync(
        IntegrationRequest request, string correlationId, CancellationToken cancellationToken);

    Task<HealthCheckResult> CheckConnectorHealthAsync(
        string configId, CancellationToken cancellationToken);
}

public class HealthCheckResult
{
    public string ConfigId { get; set; } = string.Empty;
    public bool Healthy { get; set; }
    public string? Error { get; set; }
}
