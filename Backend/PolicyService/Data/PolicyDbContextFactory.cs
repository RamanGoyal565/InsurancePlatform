using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolicyService.Data;

public sealed class PolicyDbContextFactory : IDesignTimeDbContextFactory<PolicyDbContext>
{
    public PolicyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PolicyDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=InsurancePlatform_Policy;Trusted_Connection=True;TrustServerCertificate=True");
        return new PolicyDbContext(optionsBuilder.Options);
    }
}
