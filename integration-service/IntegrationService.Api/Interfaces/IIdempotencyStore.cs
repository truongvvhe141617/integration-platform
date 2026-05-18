using IntegrationService.Api.Models;

namespace IntegrationService.Api.Interfaces;

/// <summary>
/// Store cho idempotency check.
/// Tránh duplicate request tới third-party (đặc biệt quan trọng cho payment/transfer).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Kiểm tra đã xử lý chưa, trả về cached response nếu có</summary>
    Task<IntegrationResponse?> GetAsync(string idempotencyKey);

    /// <summary>Lưu response cho idempotency key</summary>
    Task SetAsync(string idempotencyKey, IntegrationResponse response, TimeSpan? expiry = null);
}
