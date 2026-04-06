using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Authorize]
[Route("payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CreatePaymentRequest request, CancellationToken cancellationToken) => Ok(await paymentService.ProcessAsync(request, User.ToCurrentUser(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken) => Ok(await paymentService.GetAsync(cancellationToken));
}
