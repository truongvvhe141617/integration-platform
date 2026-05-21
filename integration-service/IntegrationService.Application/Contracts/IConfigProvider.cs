using BuildingBlocks.Abstractions.Connectors;

namespace IntegrationService.Application.Contracts;

/// <summary>
/// Interface để lấy config từ Config Service (có cache Redis).
/// Application layer định nghĩa contract, Infrastructure layer implement.
/// </summary>
public interface IConfigProvider
{
    Task<ConnectorConfig?> GetConfigAsync(string configId);
    Task InvalidateCacheAsync(string configId);
}
