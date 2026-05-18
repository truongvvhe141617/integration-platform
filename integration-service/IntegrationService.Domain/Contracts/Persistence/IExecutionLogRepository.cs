using IntegrationService.Domain.Entities;

namespace IntegrationService.Domain.Contracts.Persistence;

public interface IExecutionLogRepository : IAsyncRepository<ExecutionLog>
{
    Task<(IEnumerable<ExecutionLog> Items, int Total)> GetPagedAsync(
        string? tenantId, string? configKey, string? correlationId,
        bool? isSuccess, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default);

    Task<ExecutionLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
}
