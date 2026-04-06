using TicketService.DTOs;
using TicketService.Models;

namespace TicketService.Services
{
    public interface ITicketWorkflowService
    {
        Task<Ticket> CreateAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken);
        Task<IReadOnlyList<Ticket>> GetAsync(CurrentUser user, CancellationToken cancellationToken);
        Task<IReadOnlyList<Comment>> GetCommentsAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken);
        Task<Ticket> UpdateStatusAsync(Guid ticketId, UpdateTicketStatusRequest request, CurrentUser user, CancellationToken cancellationToken);
        Task<Ticket> AssignAsync(Guid ticketId, AssignTicketRequest request, CurrentUser user, CancellationToken cancellationToken);
        Task<Ticket> AddCommentAsync(Guid ticketId, AddCommentRequest request, CurrentUser user, CancellationToken cancellationToken);
        Task<Ticket> ApproveClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken);
        Task<Ticket> RejectClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken);
    }
}
