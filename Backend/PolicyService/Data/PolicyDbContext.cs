using Microsoft.EntityFrameworkCore;
using PolicyService.Models;

namespace PolicyService.Data;

public sealed class PolicyDbContext(DbContextOptions<PolicyDbContext> options) : DbContext(options)
{
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<CustomerPolicy> CustomerPolicies => Set<CustomerPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Policy>().Property(x => x.Premium).HasPrecision(18, 2);
        modelBuilder.Entity<Policy>().Property(x => x.VehicleType).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<CustomerPolicy>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<CustomerPolicy>().Property(x => x.PendingOperation).HasConversion<string>().HasMaxLength(20);
    }
}
