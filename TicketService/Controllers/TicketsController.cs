using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketService.DTOs;
using TicketService.Services;

namespace TicketService.Controllers;

[ApiController]
[Authorize]
[Route("tickets")]
public sealed class TicketsController(ITicketWorkflowService ticketWorkflowService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CreateTicketRequest request, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.CreateAsync(request, User.ToCurrentUser(), cancellationToken));

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken) => Ok(await ticketWorkflowService.GetAsync(User.ToCurrentUser(), cancellationToken));

    [HttpGet("{ticketId:guid}/comments")]
    public async Task<ActionResult> GetComments(Guid ticketId, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.GetCommentsAsync(ticketId, User.ToCurrentUser(), cancellationToken));

    [HttpPut("{ticketId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid ticketId, UpdateTicketStatusRequest request, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.UpdateStatusAsync(ticketId, request, User.ToCurrentUser(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{ticketId:guid}/assign")]
    public async Task<ActionResult> Assign(Guid ticketId, AssignTicketRequest request, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.AssignAsync(ticketId, request, User.ToCurrentUser(), cancellationToken));

    [HttpPost("{ticketId:guid}/comments")]
    public async Task<ActionResult> AddComment(Guid ticketId, AddCommentRequest request, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.AddCommentAsync(ticketId, request, User.ToCurrentUser(), cancellationToken));

    [Authorize(Roles = "ClaimsSpecialist")]
    [HttpPost("{ticketId:guid}/approve")]
    public async Task<ActionResult> Approve(Guid ticketId, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.ApproveClaimAsync(ticketId, User.ToCurrentUser(), cancellationToken));

    [Authorize(Roles = "ClaimsSpecialist")]
    [HttpPost("{ticketId:guid}/reject")]
    public async Task<ActionResult> Reject(Guid ticketId, CancellationToken cancellationToken) => Ok(await ticketWorkflowService.RejectClaimAsync(ticketId, User.ToCurrentUser(), cancellationToken));
}
