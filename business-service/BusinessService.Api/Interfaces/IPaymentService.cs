using BusinessService.Api.Models;

namespace BusinessService.Api.Interfaces;

/// <summary>
/// Service layer cho payment business logic.
/// </summary>
public interface IPaymentService
{
    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
}
