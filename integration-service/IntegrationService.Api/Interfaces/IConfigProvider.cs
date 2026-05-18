using BuildingBlocks.Abstractions.Connectors;

namespace IntegrationService.Api.Interfaces;

/// <summary>
/// Interface để lấy config từ Config Service (có cache Redis).
/// Tách riêng khỏi controller → testable, clean architecture.
/// </summary>
public interface IConfigProvider
{
    Task<ConnectorConfig?> GetConfigAsync(string configId);
    Task InvalidateCacheAsync(string configId);
}
