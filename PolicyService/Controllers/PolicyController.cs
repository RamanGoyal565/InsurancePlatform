using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyService.DTOs;
using PolicyService.Services;

namespace PolicyService.Controllers;

[ApiController]
[Route("")]
public sealed class PolicyController(IPolicyWorkflowService service) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult> GetPolicies(CancellationToken cancellationToken)
    {
        return Ok(await service.GetPoliciesAsync(cancellationToken));
    }
    [Authorize(Roles = "Customer")]
    [HttpGet("customer-policies")]
    public async Task<ActionResult> GetCustomerPolicies(CancellationToken cancellationToken)
    {
        return Ok(await service.GetCustomerPoliciesAsync(User.ToCurrentUser(), cancellationToken));
    }
    [HttpGet("policies/{policyId:guid}/document")]
    public async Task<ActionResult> GetPolicyDocument(Guid policyId, CancellationToken cancellationToken)
    {
       return Ok(await service.GetPolicyDocumentAsync(policyId, cancellationToken));
    }
    [HttpGet("internal/policies/{policyId:guid}/customers/{customerId:guid}/ticket-validation")]
    public async Task<ActionResult> ValidateTicketPolicy(Guid policyId, Guid customerId, CancellationToken cancellationToken)
    {
        return Ok(await service.ValidateTicketPolicyAsync(policyId, customerId, cancellationToken));
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("policies")]
    public async Task<ActionResult> CreatePolicy(CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.CreatePolicyAsync(request, cancellationToken));
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("policies/{policyId:guid}")]
    public async Task<ActionResult> UpdatePolicy(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdatePolicyAsync(policyId, request, cancellationToken));
    }
    [Authorize(Roles = "Customer")]
    [HttpPost("purchase")]
    public async Task<ActionResult> Purchase(PurchasePolicyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.PurchaseAsync(request, User.ToCurrentUser(), cancellationToken));
    }
    [Authorize(Roles = "Customer")]
    [HttpPost("customer-policies/{customerPolicyId:guid}/renew")]
    public async Task<ActionResult> Renew(Guid customerPolicyId, CancellationToken cancellationToken)
    {
        return Ok(await service.RenewAsync(customerPolicyId, User.ToCurrentUser(), cancellationToken));
    }
}
