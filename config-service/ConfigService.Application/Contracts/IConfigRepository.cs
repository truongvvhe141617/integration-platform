using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Application.Models;

namespace ConfigService.Application.Contracts;

/// <summary>
/// Repository interface cho config storage.
/// </summary>
public interface IConfigRepository
{
    Task<ConnectorConfig?> GetActiveAsync(string id);
    Task<ConnectorConfig?> GetByVersionAsync(string id, int version);
    Task<List<ConnectorConfig>> GetAllVersionsAsync(string id);
    Task<List<ConnectorConfig>> GetAllAsync(string? status, string? connectorType);
    Task CreateAsync(ConnectorConfig config);
    Task UpdateAsync(ConnectorConfig config);
    Task SoftDeleteAsync(string id);
}

/// <summary>
/// Repository cho audit log.
/// </summary>
public interface IConfigAuditRepository
{
    Task AddAsync(ConfigAuditEntry entry);
    Task<List<ConfigAuditEntry>> GetByConfigIdAsync(string configId);
}
