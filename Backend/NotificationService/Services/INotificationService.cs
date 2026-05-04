using NotificationService.Models;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<IReadOnlyList<Notification>> GetAsync(CurrentUser currentUser, CancellationToken cancellationToken);
        Task<Notification> MarkReadAsync(Guid notificationId, CurrentUser currentUser, bool isRead, CancellationToken cancellationToken);
    }
}
