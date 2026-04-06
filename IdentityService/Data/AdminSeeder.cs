using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        await dbContext.Database.MigrateAsync();

        var email = configuration["SeedAdmin:Email"] ?? "admin@insurance.local";
        var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existingAdmin is not null) return;

        dbContext.Users.Add(new User
        {
            Name = configuration["SeedAdmin:Name"] ?? "Platform Admin",
            Email = email,
            PasswordHash = passwordHasher.Hash(configuration["SeedAdmin:Password"] ?? "Admin@12345"),
            Role = UserRole.Admin,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }
}