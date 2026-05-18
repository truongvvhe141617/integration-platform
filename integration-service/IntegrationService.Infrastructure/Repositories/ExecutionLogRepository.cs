using IntegrationService.Domain.Contracts.Persistence;
using IntegrationService.Domain.Entities;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Repositories;

public class ExecutionLogRepository : RepositoryBase<ExecutionLog>, IExecutionLogRepository
{
    public ExecutionLogRepository(IntegrationDbContext dbContext) : base(dbContext) { }

    public async Task<(IEnumerable<ExecutionLog> Items, int Total)> GetPagedAsync(
        string? tenantId, string? configKey, string? correlationId,
        bool? isSuccess, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.ExecutionLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(tenantId)) query = query.Where(x => x.TenantId == tenantId);
        if (!string.IsNullOrEmpty(configKey)) query = query.Where(x => x.ConfigKey == configKey);
        if (!string.IsNullOrEmpty(correlationId)) query = query.Where(x => x.CorrelationId == correlationId);
        if (isSuccess.HasValue) query = query.Where(x => x.IsSuccess == isSuccess.Value);
        if (from.HasValue) query = query.Where(x => x.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.StartedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ExecutionLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
        => await _dbContext.ExecutionLogs
            .Include(x => x.RetryLogs)
            .FirstOrDefaultAsync(x => x.CorrelationId == correlationId, ct);
}
