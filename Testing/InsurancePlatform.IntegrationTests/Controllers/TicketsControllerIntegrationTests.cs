using System.Net.Http.Json;
using InsurancePlatform.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TicketService.DTOs;
using TicketService.Models;
using TicketService.Services;
using Xunit;

namespace InsurancePlatform.IntegrationTests.Controllers;

public sealed class TicketsControllerIntegrationTests
{
    [Fact]
    public async Task Create_RequiresAuthenticatedUser()
    {
        var app = await CreateAppAsync(services => services.AddSingleton<ITicketWorkflowService>(new FakeTicketWorkflowService()));
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/tickets", new CreateTicketRequest
        {
            Title = "Need help",
            Description = "desc",
            Type = TicketType.Support
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_UsesClaimsPrincipalCurrentUser()
    {
        var fake = new FakeTicketWorkflowService();
        var app = await CreateAppAsync(services => services.AddSingleton<ITicketWorkflowService>(fake));
        var client = app.GetTestClient();
        var customerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Test-UserId", customerId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");

        var response = await client.PostAsJsonAsync("/tickets", new CreateTicketRequest
        {
            Title = "Need help",
            Description = "desc",
            Type = TicketType.Support
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal(customerId, fake.LastUser?.UserId);
        Assert.Equal("Customer", fake.LastUser?.Role);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<IServiceCollection> configureServices)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(TicketService.Controllers.TicketsController).Assembly);
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

    private sealed class FakeTicketWorkflowService : ITicketWorkflowService
    {
        public CurrentUser? LastUser { get; private set; }
        public Task<Ticket> CreateAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            LastUser = user;
            return Task.FromResult(new Ticket { Title = request.Title, Description = request.Description, Type = request.Type, CustomerId = user.UserId });
        }
        public Task<IReadOnlyList<Ticket>> GetAsync(CurrentUser user, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Ticket>>([]);
        public Task<IReadOnlyList<Comment>> GetCommentsAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Comment>>([]);
        public Task<Ticket> UpdateStatusAsync(Guid ticketId, UpdateTicketStatusRequest request, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult(new Ticket { TicketId = ticketId, Title = "t", Description = "d", Type = TicketType.Support, CustomerId = user.UserId, Status = request.Status });
        public Task<Ticket> AssignAsync(Guid ticketId, AssignTicketRequest request, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult(new Ticket { TicketId = ticketId, Title = "t", Description = "d", Type = TicketType.Support, CustomerId = user.UserId, AssignedTo = request.AssignedToUserId });
        public Task<Ticket> AddCommentAsync(Guid ticketId, AddCommentRequest request, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult(new Ticket { TicketId = ticketId, Title = "t", Description = "d", Type = TicketType.Support, CustomerId = user.UserId, Comments = [new Comment { TicketId = ticketId, UserId = user.UserId, Message = request.Message }] });
        public Task<Ticket> ApproveClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult(new Ticket { TicketId = ticketId, Title = "t", Description = "d", Type = TicketType.Claim, CustomerId = user.UserId });
        public Task<Ticket> RejectClaimAsync(Guid ticketId, CurrentUser user, CancellationToken cancellationToken) => Task.FromResult(new Ticket { TicketId = ticketId, Title = "t", Description = "d", Type = TicketType.Claim, CustomerId = user.UserId });
    }
}

