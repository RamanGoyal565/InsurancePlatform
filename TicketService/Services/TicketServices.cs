using TicketService.DTOs;
using TicketService.Models;
using TicketService.Repositories;

namespace TicketService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
    public sealed record TicketPolicyValidationResult(bool PolicyExists, bool CustomerOwnsPolicy);

    public sealed class TicketWorkflowService(ITicketRepository ticketRepository, IEventPublisher eventPublisher, IPolicyValidationService policyValidationService) : ITicketWorkflowService
    {
        public async Task<Ticket> CreateAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only customers can create tickets.");
            }

            if (request.Type == TicketType.Claim && request.ClaimAmount is null)
            {
                throw new InvalidOperationException("Claim amount is required for claim tickets.");
            }

            await policyValidationService.ValidateForTicketCreationAsync(request, user, cancellationToken);

            var ticket = new Ticket
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Type = request.Type,
                Status = TicketStatus.Open,
                CustomerId = user.UserId,
                PolicyId = request.PolicyId,
                ClaimDetails = request.Type == TicketType.Claim
                    ? new ClaimDetails { ClaimAmount = request.ClaimAmount ?? 0, Documents = request.Documents ?? string.Empty }
                    : null
            };

            await ticketRepository.AddAsync(ticket, cancellationToken);
            await ticketRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("TicketCreated", new { ticket.TicketId, ticket.CustomerId, Type = ticket.Type.ToString(), ticket.PolicyId }, cancellationToken);
            return ticket;
        }

        public async Task<IReadOnlyList<Ticket>> GetAsync(CurrentUser user, CancellationToken cancellationToken)
        {
            var tickets = await ticketRepository.GetAllAsync(cancellationToken);
            return user.Role switch
            {
                "Customer" => tickets.Where(x => x.CustomerId == user.UserId).ToList(),
                "ClaimsSpecialist" => tickets.Where(x => x.Type == TicketType.Claim && x.AssignedTo == user.UserId).ToList(),
                "SupportSpecialist" => tickets.Where(x => x.Type == TicketType.Support && x.AssignedTo == user.UserId).ToList(),
                _ => tickets
            };
        }

        public async Task<IReadOnlyList<Comment>> GetCommentsAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken)
        {
            var ticket = await RequireTicket(ticketId, cancellationToken);
            EnsureRoleCanHandle(ticket, user, allowCustomerOwner: true);
            return ticket.Comments.OrderBy(x => x.CreatedAt).ToList();
        }

        public async Task<Ticket> UpdateStatusAsync(Guid ticketId, UpdateTicketStatusRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            var ticket = await RequireTicket(ticketId, cancellationToken);
            EnsureRoleCanHandle(ticket, user);
            EnsureAssignmentForStatusChange(ticket, user);
            ticket.Status = request.Status;
            ticket.UpdatedAt = DateTime.UtcNow;
            await ticketRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("TicketUpdated", new { ticket.TicketId, ticket.CustomerId, ticket.AssignedTo, Status = ticket.Status.ToString(), ChangedBy = user.UserId }, cancellationToken);
            return ticket;
        }

        public async Task<Ticket> AssignAsync(Guid ticketId, AssignTicketRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only admins can assign tickets.");
            }

            var ticket = await RequireTicket(ticketId, cancellationToken);
            ticket.AssignedTo = request.AssignedToUserId;
            ticket.Status = TicketStatus.Assigned;
            ticket.UpdatedAt = DateTime.UtcNow;
            await ticketRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("TicketAssigned", new { ticket.TicketId, ticket.CustomerId, ticket.AssignedTo, Type = ticket.Type.ToString(), ChangedBy = user.UserId }, cancellationToken);
            return ticket;
        }

        public async Task<Ticket> AddCommentAsync(Guid ticketId, AddCommentRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            var ticket = await RequireTicket(ticketId, cancellationToken);
            EnsureRoleCanHandle(ticket, user, allowCustomerOwner: true);

            await ticketRepository.AddCommentAsync(new Comment
            {
                TicketId = ticket.TicketId,
                UserId = user.UserId,
                Message = request.Message.Trim()
            }, cancellationToken);

            await ticketRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("CommentAdded", new { ticket.TicketId, ticket.CustomerId, ticket.AssignedTo, CommentedBy = user.UserId, Message = request.Message.Trim() }, cancellationToken);
            return await RequireTicket(ticketId, cancellationToken);
        }

        public Task<Ticket> ApproveClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken)
            => DecideClaimAsync(ticketId, ClaimApprovalStatus.Approved, TicketStatus.Resolved, "ClaimApproved", user, cancellationToken);

        public Task<Ticket> RejectClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken)
            => DecideClaimAsync(ticketId, ClaimApprovalStatus.Rejected, TicketStatus.Rejected, "ClaimRejected", user, cancellationToken);

        private async Task<Ticket> DecideClaimAsync(Guid ticketId, ClaimApprovalStatus decision, TicketStatus ticketStatus, string eventType, CurrentUser user, CancellationToken cancellationToken)
        {
            var ticket = await RequireTicket(ticketId, cancellationToken);
            if (ticket.Type != TicketType.Claim || ticket.ClaimDetails is null)
            {
                throw new InvalidOperationException("This ticket is not a claim.");
            }

            if (!string.Equals(user.Role, "ClaimsSpecialist", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only claims specialists can approve or reject claims.");
            }

            EnsureAssignmentForStatusChange(ticket, user);
            ticket.ClaimDetails.ApprovalStatus = decision;
            ticket.Status = ticketStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            await ticketRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync(eventType, new { ticket.TicketId, ticket.CustomerId, ticket.AssignedTo, ApprovalStatus = decision.ToString(), ChangedBy = user.UserId }, cancellationToken);
            return ticket;
        }

        private async Task<Ticket> RequireTicket(Guid ticketId, CancellationToken cancellationToken)
            => await ticketRepository.GetAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException("Ticket not found.");

        private static void EnsureAssignmentForStatusChange(Ticket ticket, CurrentUser user)
        {
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (ticket.AssignedTo != user.UserId)
                {
                    ticket.AssignedTo = user.UserId;
                }

                return;
            }

            if (ticket.AssignedTo is null)
            {
                throw new InvalidOperationException("Ticket must be assigned before its status can be changed.");
            }

            if (ticket.AssignedTo != user.UserId)
            {
                throw new UnauthorizedAccessException("Only the assigned user can change this ticket's status.");
            }
        }

        private static void EnsureRoleCanHandle(Ticket ticket, CurrentUser user, bool allowCustomerOwner = false)
        {
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase)) return;
            if (allowCustomerOwner && string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase) && ticket.CustomerId == user.UserId) return;
            if (ticket.Type == TicketType.Claim && string.Equals(user.Role, "ClaimsSpecialist", StringComparison.OrdinalIgnoreCase)) return;
            if (ticket.Type == TicketType.Support && string.Equals(user.Role, "SupportSpecialist", StringComparison.OrdinalIgnoreCase)) return;
            throw new UnauthorizedAccessException("The current role cannot operate on this ticket.");
        }
    }
}
