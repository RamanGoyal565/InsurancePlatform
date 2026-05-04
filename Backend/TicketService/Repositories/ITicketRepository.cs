using TicketService.Models;

namespace TicketService.Repositories
{
    public interface ITicketRepository
    {
        Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
        Task AddCommentAsync(Comment comment, CancellationToken cancellationToken);
        Task<Ticket?> GetAsync(Guid ticketId, CancellationToken cancellationToken);
        Task<List<Ticket>> GetAllAsync(CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
