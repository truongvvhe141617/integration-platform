using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Api.Models;

namespace ConfigService.Api.Interfaces;

/// <summary>
/// Service layer cho config management.
/// Chứa business logic: versioning, governance, audit.
/// </summary>
public interface IConfigService
{
    Task<ConnectorConfig?> GetActiveAsync(string id);
    Task<ConnectorConfig?> GetByVersionAsync(string id, int version);
    Task<List<ConnectorConfig>> GetAllVersionsAsync(string id);
    Task<List<ConnectorConfig>> GetAllAsync(string? status, string? connectorType);
    Task<ConnectorConfig> CreateAsync(ConnectorConfig config, string performedBy);
    Task<ConnectorConfig> UpdateAsync(string id, ConnectorConfig config, string performedBy);
    Task<ConnectorConfig> TransitionStatusAsync(string id, string newStatus, string performedBy);
    Task<ConnectorConfig?> RollbackAsync(string id, int version, string performedBy);
    Task DeleteAsync(string id, string performedBy);
    Task<List<ConfigAuditEntry>> GetAuditLogAsync(string configId);
    List<string> ValidateConfigSchema(ConnectorConfig config);
}
