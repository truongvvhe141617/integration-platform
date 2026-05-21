using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Infrastructure.Services;

/// <summary>
/// Idempotency store dùng Redis.
/// Đảm bảo cùng 1 request không gọi third-party 2 lần (quan trọng cho payment/transfer).
/// </summary>
public class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisIdempotencyStore> _logger;

    public RedisIdempotencyStore(IDistributedCache cache, ILogger<RedisIdempotencyStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IntegrationResponse?> GetAsync(string idempotencyKey)
    {
        var key = $"idempotency:{idempotencyKey}";
        var cached = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(cached)) return null;

        _logger.LogDebug("Idempotency cache hit: {Key}", idempotencyKey);
        return JsonSerializer.Deserialize<IntegrationResponse>(cached);
    }

    public async Task SetAsync(string idempotencyKey, IntegrationResponse response, TimeSpan? expiry = null)
    {
        var key = $"idempotency:{idempotencyKey}";
        var json = JsonSerializer.Serialize(response);

        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromHours(24)
        });

        _logger.LogDebug("Idempotency stored: {Key}", idempotencyKey);
    }
}
