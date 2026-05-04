using System.Net.Http.Json;
using InsurancePlatform.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PolicyService.DTOs;
using PolicyService.Models;
using PolicyService.Services;
using Xunit;

namespace InsurancePlatform.IntegrationTests.Controllers;

public sealed class PolicyControllerIntegrationTests
{
    [Fact]
    public async Task Purchase_RequiresAuthenticatedCustomer()
    {
        var app = await CreateAppAsync(services => services.AddSingleton<IPolicyWorkflowService>(new FakePolicyWorkflowService()));
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/purchase", new PurchasePolicyRequest
        {
            PolicyId = Guid.NewGuid(),
            VehicleNumber = "DL01AB1234",
            DrivingLicenseNumber = "LIC001",
            PaymentReference = "PAY-1"
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_PassesCurrentUserToService()
    {
        var fake = new FakePolicyWorkflowService();
        var app = await CreateAppAsync(services => services.AddSingleton<IPolicyWorkflowService>(fake));
        var client = app.GetTestClient();
        var customerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Test-UserId", customerId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");

        var response = await client.PostAsJsonAsync("/purchase", new PurchasePolicyRequest
        {
            PolicyId = Guid.NewGuid(),
            VehicleNumber = "DL01AB1234",
            DrivingLicenseNumber = "LIC001",
            PaymentReference = "PAY-1"
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal(customerId, fake.LastUser?.UserId);
        Assert.Equal("Customer", fake.LastUser?.Role);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<IServiceCollection> configureServices)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(PolicyService.Controllers.PolicyController).Assembly);
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

    private sealed class FakePolicyWorkflowService : IPolicyWorkflowService
    {
        public CurrentUser? LastUser { get; private set; }
        public Task<Policy> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken) => Task.FromResult(new Policy { Name = request.Name, VehicleType = request.VehicleType, Premium = request.Premium, CoverageDetails = request.CoverageDetails, Terms = request.Terms, PolicyDocument = request.PolicyDocument ?? "Doc" });
        public Task<Policy> UpdatePolicyAsync(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken) => Task.FromResult(new Policy { PolicyId = policyId, Name = request.Name, VehicleType = request.VehicleType, Premium = request.Premium, CoverageDetails = request.CoverageDetails, Terms = request.Terms, PolicyDocument = request.PolicyDocument ?? "Doc" });
        public Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Policy>>([]);
        public Task<IReadOnlyList<CustomerPolicyResponse>> GetCustomerPoliciesAsync(CurrentUser user, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CustomerPolicyResponse>>([]);
        public Task<PolicyDocumentResponse> GetPolicyDocumentAsync(Guid policyId, CancellationToken cancellationToken) => Task.FromResult(new PolicyDocumentResponse(policyId, "Policy", "Doc"));
        public Task<TicketPolicyValidationResponse> ValidateTicketPolicyAsync(Guid policyId, Guid customerId, CancellationToken cancellationToken) => Task.FromResult(new TicketPolicyValidationResponse(true, true));
        public Task<CustomerPolicy> PurchaseAsync(PurchasePolicyRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            LastUser = user;
            return Task.FromResult(new CustomerPolicy { PolicyId = request.PolicyId, CustomerId = user.UserId, VehicleNumber = request.VehicleNumber, DrivingLicenseNumber = request.DrivingLicenseNumber, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1), Status = CustomerPolicyStatus.Pending });
        }
        public Task<CustomerPolicy> RenewAsync(Guid customerPolicyId, CurrentUser user, CancellationToken cancellationToken)
        {
            LastUser = user;
            return Task.FromResult(new CustomerPolicy { CustomerPolicyId = customerPolicyId, PolicyId = Guid.NewGuid(), CustomerId = user.UserId, VehicleNumber = "V1", DrivingLicenseNumber = "L1", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1), Status = CustomerPolicyStatus.Renewed });
        }
    }
}

