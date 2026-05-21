using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

/// <summary>
/// Repository interface — Domain layer không biết gì về DB implementation.
/// </summary>
public interface IIntegrationConfigRepository
{
    Task<IntegrationConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationConfig?> GetByKeyAsync(string tenantId, string configKey, CancellationToken ct = default);
    Task<(IEnumerable<IntegrationConfig> Items, int Total)> GetPagedAsync(
        string tenantId, string? search, string? status, string? tags,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(IntegrationConfig config, CancellationToken ct = default);
    Task UpdateAsync(IntegrationConfig config, CancellationToken ct = default);
    Task UpdateWithOperationsAsync(IntegrationConfig config, List<IntegrationOperation> newOperations, CancellationToken ct = default);
    Task DeleteAsync(IntegrationConfig config, CancellationToken ct = default);
    Task<bool> ExistsAsync(string tenantId, string configKey, CancellationToken ct = default);
}
