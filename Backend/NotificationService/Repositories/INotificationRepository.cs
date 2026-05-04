using NotificationService.Models;

namespace NotificationService.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken cancellationToken);
        Task<List<Notification>> GetAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken);
        Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);
        Task UpsertUserContactAsync(Guid userId, string email, string? name, CancellationToken cancellationToken);
        Task<List<UserContact>> GetUserContactsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
