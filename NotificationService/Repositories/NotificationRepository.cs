using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Repositories
{
    public sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
    {
        public Task AddAsync(Notification notification, CancellationToken cancellationToken) => dbContext.Notifications.AddAsync(notification, cancellationToken).AsTask();

        public Task<List<Notification>> GetAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken)
        {
            var query = dbContext.Notifications.AsQueryable();
            if (!isAdmin) query = query.Where(x => x.UserId == userId);
            return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        }

        public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken) => dbContext.Notifications.FirstOrDefaultAsync(x => x.NotificationId == notificationId, cancellationToken);

        public async Task UpsertUserContactAsync(Guid userId, string email, string? name, CancellationToken cancellationToken)
        {
            var existing = await dbContext.UserContacts.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            if (existing is null)
            {
                await dbContext.UserContacts.AddAsync(new UserContact { UserId = userId, Email = email.Trim().ToLowerInvariant(), Name = name?.Trim(), UpdatedAt = DateTime.UtcNow }, cancellationToken);
                return;
            }

            existing.Email = email.Trim().ToLowerInvariant();
            existing.Name = string.IsNullOrWhiteSpace(name) ? existing.Name : name.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }

        public Task<List<UserContact>> GetUserContactsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
        {
            var ids = userIds.Distinct().ToList();
            return dbContext.UserContacts.Where(x => ids.Contains(x.UserId)).ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}
