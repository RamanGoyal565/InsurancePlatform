using System.Net;
using System.Net.Http.Json;
using InsurancePlatform.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InsurancePlatform.IntegrationTests.Controllers;

public sealed class ReportingControllerIntegrationTests
{
    [Fact]
    public async Task Dashboard_RequiresAdminRole()
    {
        var app = await CreateAppAsync(services => services.AddSingleton<AdminService.Services.IAdminReportingService>(new FakeAdminReportingService()));
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PolicyCustomers_ReturnsAdminOnlyPolicyCustomerReport()
    {
        var policyId = Guid.NewGuid();
        var app = await CreateAppAsync(services => services.AddSingleton<AdminService.Services.IAdminReportingService>(new FakeAdminReportingService(policyId)));
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var response = await client.GetAsync($"/reports/policies/{policyId}/customers");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AdminService.DTOs.PolicyCustomersReportResponse>();

        Assert.NotNull(payload);
        Assert.Equal(policyId, payload!.PolicyId);
        Assert.Equal(1, payload.TotalCustomers);
        Assert.Single(payload.Customers);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<IServiceCollection> configureServices)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(AdminService.Controllers.ReportingController).Assembly);
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

    private sealed class FakeAdminReportingService(Guid? policyId = null) : AdminService.Services.IAdminReportingService
    {
        private readonly Guid _policyId = policyId ?? Guid.NewGuid();

        public Task<AdminService.DTOs.DashboardSummaryResponse> GetDashboardAsync(CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.DashboardSummaryResponse(1, 1, 1, 1, 100m, 1, 1));
        public Task<AdminService.DTOs.TicketReportsResponse> GetTicketReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.TicketReportsResponse(0, [], [], new AdminService.DTOs.DateGroupedCountReport([], [], []), 0, 0, null));
        public Task<AdminService.DTOs.ClaimReportsResponse> GetClaimReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.ClaimReportsResponse(0, [], 0, 0, null, [], new AdminService.DTOs.DateGroupedCountReport([], [], [])));
        public Task<AdminService.DTOs.RevenueReportsResponse> GetRevenueReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.RevenueReportsResponse(0, new AdminService.DTOs.DateGroupedRevenueReport([], [], []), [], [], []));
        public Task<AdminService.DTOs.PolicyReportsResponse> GetPolicyReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.PolicyReportsResponse(0, 0, 0, [], new AdminService.DTOs.DateGroupedCountReport([], [], []), 0));
        public Task<AdminService.DTOs.PolicyCustomersReportResponse> GetPolicyCustomersReportAsync(Guid policyId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
            => Task.FromResult(new AdminService.DTOs.PolicyCustomersReportResponse(
                policyId,
                "Car Protect",
                "Car",
                1,
                [new AdminService.DTOs.PolicyCustomerItem(Guid.NewGuid(), "Satyam", "satyam@test.com", true, DateTime.UtcNow.AddDays(-10), 1, 0, "Active")]));
        public Task<AdminService.DTOs.UserReportsResponse> GetUserReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.UserReportsResponse(0, new AdminService.DTOs.UsersByRoleReport(0,0,0), 0, 0, new AdminService.DTOs.DateGroupedCountReport([], [], []), []));
        public Task<AdminService.DTOs.PerformanceReportsResponse> GetPerformanceReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminService.DTOs.PerformanceReportsResponse([], []));
    }
}
