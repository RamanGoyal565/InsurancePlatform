using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketService.Data;

public sealed class TicketDbContextFactory : IDesignTimeDbContextFactory<TicketDbContext>
{
    public TicketDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TicketDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=InsurancePlatform_Ticket;Trusted_Connection=True;TrustServerCertificate=True");
        return new TicketDbContext(optionsBuilder.Options);
    }
}
