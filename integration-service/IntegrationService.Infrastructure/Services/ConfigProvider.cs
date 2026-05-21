using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Abstractions.Connectors;
using IntegrationService.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Infrastructure.Services;

/// <summary>
/// Config Provider lấy config từ Config Service API, cache bằng Redis/InMemory.
/// Chuyển đổi từ DB format (flat) sang ConnectorConfig format (nested).
/// </summary>
public class ConfigProvider : IConfigProvider
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConfigProvider> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

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

        // 1. Check cache (cached as ConnectorConfig format)
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogDebug("Config cache hit: {ConfigId}", configId);
            return JsonSerializer.Deserialize<ConnectorConfig>(cached, JsonOptions);
        }

        // 2. Call Config Service
        _logger.LogDebug("Config cache miss: {ConfigId}, fetching from Config Service", configId);
        var response = await _httpClient.GetAsync($"/api/v1/integrations/by-key/{configId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Config not found: {ConfigId}, StatusCode: {Code}",
                configId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<ConfigDto>(json, JsonOptions);
        if (dto == null) return null;

        // 3. Map flat DTO → nested ConnectorConfig
        var config = MapToConnectorConfig(dto);

        // 4. Cache as ConnectorConfig format
        var configJson = JsonSerializer.Serialize(config, JsonOptions);
        await _cache.SetStringAsync(cacheKey, configJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration
        });

        return config;
    }

    public async Task InvalidateCacheAsync(string configId)
    {
        var cacheKey = $"connector-config:{configId}";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Config cache invalidated: {ConfigId}", configId);
    }

    /// <summary>Map từ DB flat format sang ConnectorConfig nested format</summary>
    private static ConnectorConfig MapToConnectorConfig(ConfigDto dto)
    {
        // Parse authParams JSON
        var authParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(dto.AuthParams))
        {
            try { authParams = JsonSerializer.Deserialize<Dictionary<string, string>>(dto.AuthParams, JsonOptions) ?? new(); }
            catch { /* ignore parse errors */ }
        }

        // Parse defaultHeaders JSON
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(dto.DefaultHeaders))
        {
            try { headers = JsonSerializer.Deserialize<Dictionary<string, string>>(dto.DefaultHeaders, JsonOptions) ?? new(); }
            catch { /* ignore */ }
        }

        return new ConnectorConfig
        {
            Id = dto.ConfigKey ?? dto.Id?.ToString() ?? "",
            Name = dto.Name ?? "",
            ConnectorType = dto.ConnectorType ?? "generic-http",
            Version = dto.Version,
            Status = dto.Status ?? "draft",
            TimeoutMs = dto.TimeoutMs > 0 ? dto.TimeoutMs : 30000,
            Endpoint = new EndpointConfig
            {
                BaseUrl = dto.BaseUrl ?? "",
                DefaultHeaders = headers,
            },
            Authentication = new AuthConfig
            {
                Type = MapAuthType(dto.AuthType),
                Parameters = authParams,
            },
            Operations = dto.Operations?.Select(op => new OperationConfig
            {
                Name = op.Name ?? "default",
                HttpMethod = op.HttpMethod ?? "POST",
                Path = op.Path ?? "/",
                ContentType = op.ContentType ?? "application/json",
                RequestMappings = op.RequestMappings?.Select(m => new FieldMapping
                {
                    Source = m.SourceField ?? "",
                    Target = m.TargetField ?? "",
                    DefaultValue = m.DefaultValue,
                    Transform = m.Transform,
                    Required = m.IsRequired,
                }).ToList() ?? new(),
                ResponseMappings = op.ResponseMappings?.Select(m => new FieldMapping
                {
                    Source = m.SourceField ?? "",
                    Target = m.TargetField ?? "",
                    DefaultValue = m.DefaultValue,
                    Transform = m.Transform,
                }).ToList() ?? new(),
                Validations = op.Validations?.Select(v => new ValidationRule
                {
                    Field = v.Field ?? "",
                    Rule = v.Rule ?? "",
                    ErrorMessage = v.ErrorMessage,
                }).ToList() ?? new(),
            }).ToList() ?? new(),
        };
    }

    private static string MapAuthType(string? authType) => authType?.ToLower() switch
    {
        "apikey" => "ApiKey",
        "basic" => "BasicAuth",
        "bearer" => "BearerToken",
        _ => "None",
    };

    // ── DTO matching API response format ──
    private class ConfigDto
    {
        public Guid? Id { get; set; }
        public string? ConfigKey { get; set; }
        public string? Name { get; set; }
        public string? ConnectorType { get; set; }
        public string? Status { get; set; }
        public int Version { get; set; }
        public string? BaseUrl { get; set; }
        public string? DefaultHeaders { get; set; }
        public string? AuthType { get; set; }
        public string? AuthParams { get; set; }
        public int TimeoutMs { get; set; }
        public List<OperationDto>? Operations { get; set; }
    }

    private class OperationDto
    {
        public string? Name { get; set; }
        public string? HttpMethod { get; set; }
        public string? Path { get; set; }
        public string? ContentType { get; set; }
        public List<MappingDto>? RequestMappings { get; set; }
        public List<MappingDto>? ResponseMappings { get; set; }
        public List<ValDto>? Validations { get; set; }
    }

    private class MappingDto
    {
        public string? SourceField { get; set; }
        public string? TargetField { get; set; }
        public string? DefaultValue { get; set; }
        public string? Transform { get; set; }
        public bool IsRequired { get; set; }
    }

    private class ValDto
    {
        public string? Field { get; set; }
        public string? Rule { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
