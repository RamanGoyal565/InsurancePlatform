using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserContact> UserContacts => Set<UserContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserContact>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
    }
}
