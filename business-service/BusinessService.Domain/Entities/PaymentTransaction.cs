using BusinessService.Domain.Common;

namespace BusinessService.Domain.Entities;

/// <summary>
/// Lưu lịch sử payment transactions.
/// DB riêng của Business Service.
/// </summary>
public class PaymentTransaction : EntityBase
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "pending"; // pending, success, failed
    public string? TransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
