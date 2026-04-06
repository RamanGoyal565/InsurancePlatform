using Microsoft.EntityFrameworkCore;
using TicketService.Models;

namespace TicketService.Data;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ClaimDetails> ClaimDetails => Set<ClaimDetails>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasMany(x => x.Comments).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ClaimDetails).WithOne(x => x.Ticket).HasForeignKey<ClaimDetails>(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimDetails>(entity =>
        {
            entity.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ClaimAmount).HasPrecision(18, 2);
        });
    }
}

