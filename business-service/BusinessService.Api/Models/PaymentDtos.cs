namespace BusinessService.Api.Models;

public class PaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Description { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Error { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
