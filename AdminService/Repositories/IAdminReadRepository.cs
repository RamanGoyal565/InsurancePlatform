using AdminService.Models;

namespace AdminService.Repositories
{
    public interface IAdminReadRepository
    {
        Task AddAsync(EventAudit audit, CancellationToken cancellationToken);
        Task<List<EventAudit>> GetAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }

}
