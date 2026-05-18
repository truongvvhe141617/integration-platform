using System.Text.Json;
using BuildingBlocks.Abstractions.Connectors;
using IntegrationService.Api.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace IntegrationService.Api.Services;

/// <summary>
/// Config Provider lấy config từ Config Service API, cache bằng Redis/InMemory.
/// Flow: Check cache → nếu miss → gọi Config Service API → cache lại.
/// </summary>
public class ConfigProvider : IConfigProvider
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConfigProvider> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    // ASP.NET Core trả JSON camelCase, C# properties là PascalCase
    // → PHẢI bật PropertyNameCaseInsensitive
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConfigProvider(
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        ILogger<ConfigProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ConfigService");
        _cache = cache;
        _logger = logger;
    }

    public async Task<ConnectorConfig?> GetConfigAsync(string configId)
    {
        var cacheKey = $"connector-config:{configId}";

        // 1. Check cache
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogDebug("Config cache hit: {ConfigId}", configId);
            return JsonSerializer.Deserialize<ConnectorConfig>(cached, JsonOptions);
        }

        // 2. Call Config Service
        _logger.LogDebug("Config cache miss: {ConfigId}, fetching from Config Service", configId);
        var response = await _httpClient.GetAsync($"/api/v1/configs/{configId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Config not found: {ConfigId}, StatusCode: {Code}",
                configId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var config = JsonSerializer.Deserialize<ConnectorConfig>(json, JsonOptions);

        // 3. Cache result
        if (config != null)
        {
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            });
        }

        return config;
    }

    public async Task InvalidateCacheAsync(string configId)
    {
        var cacheKey = $"connector-config:{configId}";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Config cache invalidated: {ConfigId}", configId);
    }
}
