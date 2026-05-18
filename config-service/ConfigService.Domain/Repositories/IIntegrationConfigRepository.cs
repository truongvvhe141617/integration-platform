using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

/// <summary>
/// Repository interface — Domain layer không biết gì về DB implementation.
/// </summary>
public interface IIntegrationConfigRepository
{
    Task<IntegrationConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationConfig?> GetByKeyAsync(Guid tenantId, string configKey, CancellationToken ct = default);
    Task<(IEnumerable<IntegrationConfig> Items, int Total)> GetPagedAsync(
        Guid tenantId, string? search, string? status, string? tags,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(IntegrationConfig config, CancellationToken ct = default);
    Task UpdateAsync(IntegrationConfig config, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid tenantId, string configKey, CancellationToken ct = default);
}
