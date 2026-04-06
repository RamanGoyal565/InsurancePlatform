using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Repositories
{
    public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) => dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) => dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        public Task<List<User>> GetAllAsync(CancellationToken cancellationToken) => dbContext.Users.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        public Task AddAsync(User user, CancellationToken cancellationToken) => dbContext.Users.AddAsync(user, cancellationToken).AsTask();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}