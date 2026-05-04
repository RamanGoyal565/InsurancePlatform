using System.Net.Http.Json;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using InsurancePlatform.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InsurancePlatform.IntegrationTests.Controllers;

public sealed class IdentityControllerIntegrationTests
{
    [Fact]
    public async Task Register_AllowsAnonymousRequest_AndReturnsPayload()
    {
        var fake = new FakeIdentityService();
        var app = await CreateAppAsync(services => services.AddSingleton<IIdentityService>(fake));
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Name = "Satyam",
            Email = "satyam@test.com",
            Password = "Satyam@123"
        });

        response.EnsureSuccessStatusCode();
        Assert.NotNull(fake.LastRegisterRequest);
    }

    [Fact]
    public async Task AdminUsers_RequiresAdminRole()
    {
        var app = await CreateAppAsync(services => services.AddSingleton<IIdentityService>(new FakeIdentityService()));
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");

        var response = await client.GetAsync("/admin/users");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_WithAdminRole_ReturnsUsers()
    {
        var fake = new FakeIdentityService();
        var app = await CreateAppAsync(services => services.AddSingleton<IIdentityService>(fake));
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", nameof(UserRole.Admin));

        var response = await client.GetAsync("/admin/users");

        response.EnsureSuccessStatusCode();
    }

    private static async Task<WebApplication> CreateAppAsync(Action<IServiceCollection> configureServices)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(IdentityService.Controllers.AuthController).Assembly);
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        configureServices(builder.Services);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public RegisterRequest? LastRegisterRequest { get; private set; }
        public Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            LastRegisterRequest = request;
            return Task.FromResult(new AuthResponse("token", DateTime.UtcNow.AddHours(1), new UserResponse(Guid.NewGuid(), request.Name, request.Email, "Customer", true, DateTime.UtcNow)));
        }

        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new AuthResponse("token", DateTime.UtcNow.AddHours(1), new UserResponse(Guid.NewGuid(), "User", request.Email, "Customer", true, DateTime.UtcNow)));

        public Task<UserResponse> CreateUserAsync(CreateUserRequest request, UserRole role, CancellationToken cancellationToken)
            => Task.FromResult(new UserResponse(Guid.NewGuid(), request.Name, request.Email, role.ToString(), true, DateTime.UtcNow));

        public Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<UserResponse>>([new UserResponse(Guid.NewGuid(), "Admin", "admin@test.com", "Admin", true, DateTime.UtcNow)]);

        public Task<UserResponse> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
            => Task.FromResult(new UserResponse(userId, "User", "user@test.com", "Customer", isActive, DateTime.UtcNow));
    }
}

