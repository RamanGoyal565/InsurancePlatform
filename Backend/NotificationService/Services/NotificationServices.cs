using NotificationService.Models;
using NotificationService.Repositories;

namespace NotificationService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);

    public sealed class NotificationService(INotificationRepository repository) : INotificationService
    {
        public Task<IReadOnlyList<Notification>> GetAsync(CurrentUser currentUser, CancellationToken cancellationToken) => repository.GetAsync(currentUser.UserId, string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase), cancellationToken).ContinueWith<IReadOnlyList<Notification>>(t => t.Result, cancellationToken);

        public async Task<Notification> MarkReadAsync(Guid notificationId, CurrentUser currentUser, bool isRead, CancellationToken cancellationToken)
        {
            var notification = await repository.GetByIdAsync(notificationId, cancellationToken) ?? throw new KeyNotFoundException("Notification not found.");
            if (!string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) && notification.UserId != currentUser.UserId) throw new UnauthorizedAccessException("You cannot modify this notification.");
            notification.IsRead = isRead;
            await repository.SaveChangesAsync(cancellationToken);
            return notification;
        }
    }
}
