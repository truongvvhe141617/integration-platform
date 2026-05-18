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
        => await _db.IntegrationConfigs
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IntegrationConfig?> GetByKeyAsync(Guid tenantId, string configKey, CancellationToken ct = default)
        => await _db.IntegrationConfigs
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ConfigKey == configKey, ct);

    public async Task<(IEnumerable<IntegrationConfig> Items, int Total)> GetPagedAsync(
        Guid tenantId, string? search, string? status, string? tags,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.IntegrationConfigs
            .Where(c => c.TenantId == tenantId);

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
        _db.IntegrationConfigs.Update(config);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string configKey, CancellationToken ct = default)
        => await _db.IntegrationConfigs
            .AnyAsync(c => c.TenantId == tenantId && c.ConfigKey == configKey, ct);
}
