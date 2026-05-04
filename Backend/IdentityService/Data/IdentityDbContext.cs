using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<OtpToken>(entity =>
        {
            entity.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.UserId, x.Purpose });
        });
    }
}
