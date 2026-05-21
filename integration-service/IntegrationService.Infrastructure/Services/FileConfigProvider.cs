using System.Collections.Concurrent;
using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;
using IntegrationService.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Infrastructure.Services;

/// <summary>
/// Config provider đọc từ JSON files — KHÔNG CẦN DB, KHÔNG CẦN CONFIG SERVICE.
/// 
/// Cấu trúc thư mục:
///   configs/
///     ├── bank-b-transfer.json
///     ├── bank-c-transfer.json
///     └── ewallet-payment.json
/// 
/// File name = connectorConfigId (không cần .json khi gọi)
/// Tự động reload khi file thay đổi (FileSystemWatcher)
/// </summary>
public class FileConfigProvider : IConfigProvider, IDisposable
{
    private readonly string _configDirectory;
    private readonly ILogger<FileConfigProvider> _logger;
    private readonly ConcurrentDictionary<string, ConnectorConfig> _cache = new();
    private readonly FileSystemWatcher? _watcher;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public FileConfigProvider(ILogger<FileConfigProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configDirectory = configuration["Connectors:ConfigDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "configs");

        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            _logger.LogInformation("Created config directory: {Dir}", _configDirectory);
        }

        LoadAllConfigs();

        _watcher = new FileSystemWatcher(_configDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        _watcher.Changed += (_, e) => ReloadConfig(e.FullPath);
        _watcher.Created += (_, e) => ReloadConfig(e.FullPath);
        _watcher.Deleted += (_, e) =>
        {
            var id = Path.GetFileNameWithoutExtension(e.Name) ?? "";
            _cache.TryRemove(id, out ConnectorConfig? _);
            _logger.LogInformation("Config removed: {Id}", id);
        };

        _logger.LogInformation("FileConfigProvider initialized. Directory: {Dir}, Configs loaded: {Count}",
            _configDirectory, _cache.Count);
    }

    public Task<ConnectorConfig?> GetConfigAsync(string configId)
    {
        _cache.TryGetValue(configId, out var config);
        return Task.FromResult(config);
    }

    public Task InvalidateCacheAsync(string configId)
    {
        var filePath = Path.Combine(_configDirectory, $"{configId}.json");
        if (File.Exists(filePath))
            ReloadConfig(filePath);
        else
            _cache.TryRemove(configId, out ConnectorConfig? _);

        return Task.CompletedTask;
    }

    private void LoadAllConfigs()
    {
        foreach (var file in Directory.GetFiles(_configDirectory, "*.json"))
        {
            try { LoadConfigFromFile(file); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to load config: {File}", file); }
        }
    }

    private void ReloadConfig(string filePath)
    {
        try
        {
            Thread.Sleep(100);
            LoadConfigFromFile(filePath);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to reload config: {File}", filePath); }
    }

    private void LoadConfigFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<ConnectorConfig>(json, JsonOptions);

        if (config == null)
        {
            _logger.LogWarning("Empty config file: {File}", filePath);
            return;
        }

        if (string.IsNullOrEmpty(config.Id))
            config.Id = Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrEmpty(config.Status))
            config.Status = "Active";

        _cache.AddOrUpdate(config.Id, config, (_, _) => config);
        _logger.LogInformation("Config loaded: {Id} ({Name}) from {File}",
            config.Id, config.Name, Path.GetFileName(filePath));
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
