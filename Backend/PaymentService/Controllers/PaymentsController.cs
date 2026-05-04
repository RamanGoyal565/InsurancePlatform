using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Services;
using System.Security.Claims;

namespace PaymentService.Controllers;

[ApiController]
[Authorize]
[Route("payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
        => Ok(await paymentService.ProcessAsync(request, User.ToCurrentUser(), cancellationToken));

    /// <summary>Admin: get all payments.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
        => Ok(await paymentService.GetAsync(cancellationToken));

    /// <summary>Customer: get their own payments.</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<ActionResult> GetMy(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out var customerId))
            return Unauthorized();

        return Ok(await paymentService.GetMyAsync(customerId, cancellationToken));
    }
}
