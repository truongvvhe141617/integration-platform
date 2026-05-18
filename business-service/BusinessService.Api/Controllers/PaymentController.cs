using BusinessService.Api.Interfaces;
using BusinessService.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

/// <summary>
/// Thin controller — delegate sang IPaymentService.
/// </summary>
[ApiController]
[Route("api/v1/business/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] PaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(request, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
