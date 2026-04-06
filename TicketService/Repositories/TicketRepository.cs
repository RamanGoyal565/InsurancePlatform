using Microsoft.EntityFrameworkCore;
using TicketService.Data;
using TicketService.Models;

namespace TicketService.Repositories
{
    public sealed class TicketRepository(TicketDbContext dbContext) : ITicketRepository
    {
        public Task AddAsync(Ticket ticket, CancellationToken cancellationToken) => dbContext.Tickets.AddAsync(ticket, cancellationToken).AsTask();
        public Task AddCommentAsync(Comment comment, CancellationToken cancellationToken) => dbContext.Comments.AddAsync(comment, cancellationToken).AsTask();

        public Task<Ticket?> GetAsync(Guid ticketId, CancellationToken cancellationToken) => dbContext.Tickets
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .Include(x => x.ClaimDetails)
            .FirstOrDefaultAsync(x => x.TicketId == ticketId, cancellationToken);

        public Task<List<Ticket>> GetAllAsync(CancellationToken cancellationToken) => dbContext.Tickets
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .Include(x => x.ClaimDetails)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}