using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<EventAudit> EventAudits => Set<EventAudit>();
}
