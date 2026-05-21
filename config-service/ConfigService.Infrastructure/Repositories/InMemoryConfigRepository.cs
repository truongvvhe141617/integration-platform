using System.Collections.Concurrent;
using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Application.Contracts;
using ConfigService.Application.Models;

namespace ConfigService.Infrastructure.Repositories;

/// <summary>
/// InMemory implementation — chạy được ngay không cần SQL Server.
/// Production thay bằng EF Core + SQL Server.
/// </summary>
public class InMemoryConfigRepository : IConfigRepository
{
    private readonly ConcurrentDictionary<string, ConnectorConfig> _store = new();

    public Task<ConnectorConfig?> GetActiveAsync(string id)
    {
        var config = _store.Values
            .Where(c => c.Id == id && c.Status == "Active")
            .OrderByDescending(c => c.Version)
            .FirstOrDefault();
        return Task.FromResult(config);
    }

    public Task<ConnectorConfig?> GetByVersionAsync(string id, int version)
    {
        _store.TryGetValue(BuildKey(id, version), out var config);
        return Task.FromResult(config);
    }

    public Task<List<ConnectorConfig>> GetAllVersionsAsync(string id)
    {
        var versions = _store.Values
            .Where(c => c.Id == id)
            .OrderByDescending(c => c.Version)
            .ToList();
        return Task.FromResult(versions);
    }

    public Task<List<ConnectorConfig>> GetAllAsync(string? status, string? connectorType)
    {
        var query = _store.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(connectorType))
            query = query.Where(c => c.ConnectorType.Equals(connectorType, StringComparison.OrdinalIgnoreCase));

        var configs = query
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.Version).First())
            .ToList();

        return Task.FromResult(configs);
    }

    public Task CreateAsync(ConnectorConfig config)
    {
        var key = BuildKey(config.Id, config.Version);
        _store[key] = config;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ConnectorConfig config)
    {
        var key = BuildKey(config.Id, config.Version);
        _store[key] = config;
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(string id)
    {
        foreach (var kvp in _store.Where(x => x.Value.Id == id))
            kvp.Value.Status = "Deleted";
        return Task.CompletedTask;
    }

    private static string BuildKey(string id, int version) => $"{id}:v{version}";
}

/// <summary>
/// InMemory audit repository.
/// </summary>
public class InMemoryConfigAuditRepository : IConfigAuditRepository
{
    private readonly ConcurrentBag<ConfigAuditEntry> _entries = new();

    public Task AddAsync(ConfigAuditEntry entry)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<List<ConfigAuditEntry>> GetByConfigIdAsync(string configId)
    {
        var entries = _entries
            .Where(e => e.ConfigId == configId)
            .OrderByDescending(e => e.Timestamp)
            .ToList();
        return Task.FromResult(entries);
    }
}
