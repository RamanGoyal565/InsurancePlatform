using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Repositories;

public sealed class AdminReadRepository(AdminDbContext dbContext) : IAdminReadRepository
{
    public Task AddAsync(EventAudit audit, CancellationToken cancellationToken) => dbContext.EventAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task<List<EventAudit>> GetAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.EventAudits.AsQueryable();
        if (fromUtc.HasValue) query = query.Where(x => x.OccurredOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.OccurredOnUtc <= toUtc.Value);
        return query.OrderByDescending(x => x.OccurredOnUtc).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
