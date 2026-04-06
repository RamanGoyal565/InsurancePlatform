using IdentityService.Config;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

using IdentityWorkflow = IdentityService.Services.IdentityService;

namespace InsurancePlatform.UnitTests.Identity;

public sealed class PasswordHasherServiceTests
{
    [Fact]
    public void Hash_And_Verify_RoundTripsSuccessfully()
    {
        var sut = new PasswordHasherService();

        var hash = sut.Hash("Satyam@123");

        Assert.True(sut.Verify("Satyam@123", hash));
        Assert.False(sut.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForInvalidStoredHashFormat()
    {
        var sut = new PasswordHasherService();

        Assert.False(sut.Verify("Satyam@123", "invalid-hash-format"));
    }
}

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_EmbedsExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "InsurancePlatform",
            Audience = "InsurancePlatformClients",
            Key = "super-secret-development-key-change-me-12345",
            ExpiryMinutes = 60
        });
        var sut = new JwtTokenService(options);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = "Satyam",
            Email = "satyam@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var response = sut.CreateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal(user.UserId.ToString(), token.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, token.Claims.First(x => x.Type == ClaimTypes.Email).Value);
        Assert.Equal("Customer", token.Claims.First(x => x.Type == ClaimTypes.Role).Value);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }
}

public sealed class IdentityServiceTests
{
    [Fact]
    public async Task RegisterCustomerAsync_CreatesCustomerAndPublishesEvent()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasherService();
        var jwt = new FakeJwtTokenService();
        var events = new FakeIdentityEventPublisher();
        var sut = new IdentityWorkflow(repository, hasher, jwt, events);

        var response = await sut.RegisterCustomerAsync(new RegisterRequest
        {
            Name = " Satyam ",
            Email = " SATYAM@Test.com ",
            Password = "Satyam@123"
        }, CancellationToken.None);

        Assert.Single(repository.Users);
        Assert.Equal("satyam@test.com", repository.Users[0].Email);
        Assert.Equal(UserRole.Customer, repository.Users[0].Role);
        Assert.Equal("hashed:Satyam@123", repository.Users[0].PasswordHash);
        Assert.Equal("UserRegistered", events.Published.Single().EventType);
        Assert.Equal(response.User.Email, repository.Users[0].Email);
    }

    [Fact]
    public async Task RegisterCustomerAsync_ThrowsForDuplicateEmail()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            Name = "Existing",
            Email = "satyam@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        });
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterCustomerAsync(new RegisterRequest
        {
            Name = "Satyam",
            Email = "SATYAM@test.com",
            Password = "Satyam@123"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenForActiveUser()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            Name = "Satyam",
            Email = "satyam@test.com",
            PasswordHash = "hashed:Satyam@123",
            Role = UserRole.Customer,
            IsActive = true
        });
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        var response = await sut.LoginAsync(new LoginRequest
        {
            Email = " SATYAM@test.com ",
            Password = "Satyam@123"
        }, CancellationToken.None);

        Assert.Equal("token", response.AccessToken);
        Assert.Equal("satyam@test.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForInactiveUser()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            Name = "Satyam",
            Email = "satyam@test.com",
            PasswordHash = "hashed:Satyam@123",
            Role = UserRole.Customer,
            IsActive = false
        });
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "satyam@test.com",
            Password = "Satyam@123"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_ThrowsForInvalidPassword()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            Name = "Satyam",
            Email = "satyam@test.com",
            PasswordHash = "hashed:Satyam@123",
            Role = UserRole.Customer,
            IsActive = true
        });
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "satyam@test.com",
            Password = "Wrong@123"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserAsync_RejectsUnsupportedRole()
    {
        var sut = new IdentityWorkflow(new FakeUserRepository(), new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateUserAsync(new CreateUserRequest
        {
            Name = "Admin",
            Email = "admin@test.com",
            Password = "Admin@123"
        }, UserRole.Admin, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserAsync_CreatesSpecialistAndPublishesEvent()
    {
        var repository = new FakeUserRepository();
        var events = new FakeIdentityEventPublisher();
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), events);

        var response = await sut.CreateUserAsync(new CreateUserRequest
        {
            Name = "Support One",
            Email = "support@test.com",
            Password = "Support@123"
        }, UserRole.SupportSpecialist, CancellationToken.None);

        Assert.Equal("SupportSpecialist", response.Role);
        Assert.Single(repository.Users);
        Assert.Equal("UserRegistered", events.Published.Single().EventType);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsMappedUsers()
    {
        var repository = new FakeUserRepository();
        repository.Users.AddRange(
        [
            new User { UserId = Guid.NewGuid(), Name = "A", Email = "a@test.com", PasswordHash = "hash", Role = UserRole.Customer },
            new User { UserId = Guid.NewGuid(), Name = "B", Email = "b@test.com", PasswordHash = "hash", Role = UserRole.ClaimsSpecialist }
        ]);
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        var users = await sut.GetUsersAsync(CancellationToken.None);

        Assert.Equal(2, users.Count);
        Assert.Contains(users, x => x.Role == "Customer");
        Assert.Contains(users, x => x.Role == "ClaimsSpecialist");
    }

    [Fact]
    public async Task UpdateUserStatusAsync_PublishesUserStatusUpdated()
    {
        var repository = new FakeUserRepository();
        var existing = new User
        {
            UserId = Guid.NewGuid(),
            Name = "Support One",
            Email = "support@test.com",
            PasswordHash = "hash",
            Role = UserRole.SupportSpecialist,
            IsActive = true
        };
        repository.Users.Add(existing);
        var events = new FakeIdentityEventPublisher();
        var sut = new IdentityWorkflow(repository, new FakePasswordHasherService(), new FakeJwtTokenService(), events);

        var response = await sut.UpdateUserStatusAsync(existing.UserId, false, CancellationToken.None);

        Assert.False(response.IsActive);
        Assert.Equal("UserStatusUpdated", events.Published.Single().EventType);
        Assert.False(repository.Users.Single().IsActive);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_ThrowsWhenUserMissing()
    {
        var sut = new IdentityWorkflow(new FakeUserRepository(), new FakePasswordHasherService(), new FakeJwtTokenService(), new FakeIdentityEventPublisher());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateUserStatusAsync(Guid.NewGuid(), false, CancellationToken.None));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Users.Any(x => x.Email == email));
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Users.SingleOrDefault(x => x.Email == email));
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(Users.SingleOrDefault(x => x.UserId == userId));
        public Task<List<User>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(Users.ToList());
        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasherService : IPasswordHasherService
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string storedHash) => storedHash == $"hashed:{password}";
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public AuthResponse CreateToken(User user) => new("token", DateTime.UtcNow.AddHours(1), new UserResponse(user.UserId, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt));
    }

    private sealed class FakeIdentityEventPublisher : IEventPublisher
    {
        public List<(string EventType, object Payload)> Published { get; } = [];
        public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            Published.Add((eventType, payload));
            return Task.CompletedTask;
        }
    }
}
