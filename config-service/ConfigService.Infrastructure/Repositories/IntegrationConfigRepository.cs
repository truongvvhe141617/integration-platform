using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using ConfigService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfigService.Infrastructure.Repositories;

public class IntegrationConfigRepository : IIntegrationConfigRepository
{
    private readonly AppDbContext _db;

    public IntegrationConfigRepository(AppDbContext db) => _db = db;

    public async Task<IntegrationConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.IntegrationConfigs.IgnoreQueryFilters()
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IntegrationConfig?> GetByKeyAsync(string tenantId, string configKey, CancellationToken ct = default)
        => await _db.IntegrationConfigs
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == configKey, ct);

    public async Task<(IEnumerable<IntegrationConfig> Items, int Total)> GetPagedAsync(
        string tenantId, string? search, string? status, string? tags,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.IntegrationConfigs.IgnoreQueryFilters()
            .Where(c => c.IsDeleted == false);

        if (!string.IsNullOrEmpty(tenantId) && tenantId != "default")
            query = query.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.ConfigKey.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(tags))
            query = query.Where(c => c.Tags != null && c.Tags.Contains(tags));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(IntegrationConfig config, CancellationToken ct = default)
    {
        await _db.IntegrationConfigs.AddAsync(config, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(IntegrationConfig config, CancellationToken ct = default)
    {
        _db.Entry(config).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateWithOperationsAsync(IntegrationConfig config, List<IntegrationOperation> newOperations, CancellationToken ct = default)
    {
        // 1. Detach all tracked entities to avoid conflicts
        _db.ChangeTracker.Clear();

        // 2. Delete old operations via raw SQL
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM request_mapping WHERE OperationId IN (SELECT Id FROM integration_operation WHERE ConfigId = {0})", config.Id);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM response_mapping WHERE OperationId IN (SELECT Id FROM integration_operation WHERE ConfigId = {0})", config.Id);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM validation_rule WHERE OperationId IN (SELECT Id FROM integration_operation WHERE ConfigId = {0})", config.Id);
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM integration_operation WHERE ConfigId = {0}", config.Id);

        // 3. Update config fields (attach as modified)
        _db.IntegrationConfigs.Attach(config);
        _db.Entry(config).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);

        // 4. Insert new operations
        if (newOperations.Count > 0)
        {
            await _db.Set<IntegrationOperation>().AddRangeAsync(newOperations, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(IntegrationConfig config, CancellationToken ct = default)
    {
        _db.IntegrationConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string tenantId, string configKey, CancellationToken ct = default)
        => await _db.IntegrationConfigs
            .AnyAsync(c => c.TenantId == tenantId && c.ConfigKey == configKey, ct);
}
