using PolicyService.DTOs;
using PolicyService.Models;
using PolicyService.Repositories;
using PolicyService.Services;
using Xunit;

using PolicyWorkflow = PolicyService.Services.PolicyWorkflowService;

namespace InsurancePlatform.UnitTests.Policy;

public sealed class PolicyWorkflowServiceTests
{
    [Fact]
    public async Task CreatePolicyAsync_PublishesPolicyCreated()
    {
        var repository = new FakePolicyRepository();
        var publisher = new FakePolicyEventPublisher();
        var sut = new PolicyWorkflow(repository, publisher);

        var policy = await sut.CreatePolicyAsync(new CreatePolicyRequest
        {
            Name = "Car Protect",
            VehicleType = VehicleType.Car,
            Premium = 1200,
            CoverageDetails = "Coverage",
            Terms = "Terms"
        }, CancellationToken.None);

        Assert.Single(repository.Policies);
        Assert.Equal("PolicyCreated", publisher.Published.Single().EventType);
        Assert.Contains("Vehicle Category: Car", policy.PolicyDocument);
    }

    [Fact]
    public async Task UpdatePolicyAsync_PublishesPolicyUpdated()
    {
        var repository = new FakePolicyRepository();
        var policy = new PolicyService.Models.Policy
        {
            PolicyId = Guid.NewGuid(),
            Name = "Old",
            VehicleType = VehicleType.Bike,
            Premium = 100,
            CoverageDetails = "Old",
            Terms = "Old",
            PolicyDocument = "Old"
        };
        repository.Policies.Add(policy);
        var publisher = new FakePolicyEventPublisher();
        var sut = new PolicyWorkflow(repository, publisher);

        var updated = await sut.UpdatePolicyAsync(policy.PolicyId, new UpdatePolicyRequest
        {
            Name = "New",
            VehicleType = VehicleType.Car,
            Premium = 200,
            CoverageDetails = "Coverage",
            Terms = "Terms",
            PolicyDocument = "Doc"
        }, CancellationToken.None);

        Assert.Equal("New", updated.Name);
        Assert.Equal("PolicyUpdated", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task GetCustomerPoliciesAsync_ThrowsForNonCustomer()
    {
        var sut = new PolicyWorkflow(new FakePolicyRepository(), new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetCustomerPoliciesAsync(new CurrentUser(Guid.NewGuid(), "Admin"), CancellationToken.None));
    }

    [Fact]
    public async Task GetPolicyDocumentAsync_ThrowsForMissingPolicy()
    {
        var sut = new PolicyWorkflow(new FakePolicyRepository(), new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetPolicyDocumentAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateTicketPolicyAsync_ReturnsFalseWhenPolicyMissing()
    {
        var sut = new PolicyWorkflow(new FakePolicyRepository(), new FakePolicyEventPublisher());

        var result = await sut.ValidateTicketPolicyAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.PolicyExists);
        Assert.False(result.CustomerOwnsPolicy);
    }

    [Fact]
    public async Task ValidateTicketPolicyAsync_ReturnsFalseForCancelledOwnership()
    {
        var repository = new FakePolicyRepository();
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        repository.Policies.Add(new PolicyService.Models.Policy
        {
            PolicyId = policyId,
            Name = "Car Protect",
            VehicleType = VehicleType.Car,
            Premium = 1200,
            CoverageDetails = "Coverage",
            Terms = "Terms",
            PolicyDocument = "Doc"
        });
        repository.CustomerPolicies.Add(new CustomerPolicy
        {
            CustomerPolicyId = Guid.NewGuid(),
            PolicyId = policyId,
            CustomerId = customerId,
            VehicleNumber = "DL01AB1234",
            DrivingLicenseNumber = "LIC123",
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(11),
            Status = CustomerPolicyStatus.Cancelled
        });
        var sut = new PolicyWorkflow(repository, new FakePolicyEventPublisher());

        var result = await sut.ValidateTicketPolicyAsync(policyId, customerId, CancellationToken.None);

        Assert.True(result.PolicyExists);
        Assert.False(result.CustomerOwnsPolicy);
    }

    [Fact]
    public async Task PurchaseAsync_ThrowsForNonCustomer()
    {
        var sut = new PolicyWorkflow(new FakePolicyRepository(), new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.PurchaseAsync(new PurchasePolicyRequest
        {
            PolicyId = Guid.NewGuid(),
            VehicleNumber = "DL01AB1234",
            DrivingLicenseNumber = "LIC123",
            PaymentReference = "PAY-1"
        }, new CurrentUser(Guid.NewGuid(), "Admin"), CancellationToken.None));
    }

    [Fact]
    public async Task PurchaseAsync_CreatesPendingPolicyAndPublishesPaymentRequested()
    {
        var repository = new FakePolicyRepository();
        var policy = new PolicyService.Models.Policy
        {
            PolicyId = Guid.NewGuid(),
            Name = "Bike Shield",
            VehicleType = VehicleType.Bike,
            Premium = 999,
            CoverageDetails = "Coverage",
            Terms = "Terms",
            PolicyDocument = "Doc"
        };
        repository.Policies.Add(policy);
        var publisher = new FakePolicyEventPublisher();
        var sut = new PolicyWorkflow(repository, publisher);
        var customer = new CurrentUser(Guid.NewGuid(), "Customer");

        var result = await sut.PurchaseAsync(new PurchasePolicyRequest
        {
            PolicyId = policy.PolicyId,
            VehicleNumber = "DL01AB1234",
            DrivingLicenseNumber = "LIC123",
            PaymentReference = "PAY-1"
        }, customer, CancellationToken.None);

        Assert.Equal(CustomerPolicyStatus.Pending, result.Status);
        Assert.Equal(PolicyPaymentOperation.Purchase, result.PendingOperation);
        Assert.Equal("PaymentRequested", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task RenewAsync_RejectsWrongCustomer()
    {
        var repository = new FakePolicyRepository();
        var ownerId = Guid.NewGuid();
        var customerPolicy = CreateRenewablePolicy(ownerId, DateTime.UtcNow.Date.AddDays(20));
        repository.CustomerPolicies.Add(customerPolicy);
        var sut = new PolicyWorkflow(repository, new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RenewAsync(customerPolicy.CustomerPolicyId, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task RenewAsync_RejectsPendingPayment()
    {
        var repository = new FakePolicyRepository();
        var customerId = Guid.NewGuid();
        var customerPolicy = CreateRenewablePolicy(customerId, DateTime.UtcNow.Date.AddDays(20));
        customerPolicy.PendingOperation = PolicyPaymentOperation.Renewal;
        customerPolicy.Status = CustomerPolicyStatus.Pending;
        repository.CustomerPolicies.Add(customerPolicy);
        var sut = new PolicyWorkflow(repository, new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenewAsync(customerPolicy.CustomerPolicyId, new CurrentUser(customerId, "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task RenewAsync_RejectsTooLateAfterGracePeriod()
    {
        var repository = new FakePolicyRepository();
        var customerId = Guid.NewGuid();
        var customerPolicy = CreateRenewablePolicy(customerId, DateTime.UtcNow.Date.AddDays(-20));
        repository.CustomerPolicies.Add(customerPolicy);
        var sut = new PolicyWorkflow(repository, new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenewAsync(customerPolicy.CustomerPolicyId, new CurrentUser(customerId, "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task RenewAsync_RejectsTooEarlyRenewal()
    {
        var repository = new FakePolicyRepository();
        var customerId = Guid.NewGuid();
        var customerPolicy = CreateRenewablePolicy(customerId, DateTime.UtcNow.Date.AddMonths(3));
        repository.CustomerPolicies.Add(customerPolicy);
        var sut = new PolicyWorkflow(repository, new FakePolicyEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenewAsync(customerPolicy.CustomerPolicyId, new CurrentUser(customerId, "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task RenewAsync_SetsPendingRenewalAndPublishesPaymentRequest()
    {
        var repository = new FakePolicyRepository();
        var customerId = Guid.NewGuid();
        var customerPolicy = CreateRenewablePolicy(customerId, DateTime.UtcNow.Date.AddDays(20));
        repository.CustomerPolicies.Add(customerPolicy);
        var publisher = new FakePolicyEventPublisher();
        var sut = new PolicyWorkflow(repository, publisher);

        var result = await sut.RenewAsync(customerPolicy.CustomerPolicyId, new CurrentUser(customerId, "Customer"), CancellationToken.None);

        Assert.Equal(CustomerPolicyStatus.Pending, result.Status);
        Assert.Equal(PolicyPaymentOperation.Renewal, result.PendingOperation);
        Assert.Equal("PaymentRequested", publisher.Published.Single().EventType);
    }

    private static CustomerPolicy CreateRenewablePolicy(Guid customerId, DateTime endDate) => new()
    {
        CustomerPolicyId = Guid.NewGuid(),
        CustomerId = customerId,
        PolicyId = Guid.NewGuid(),
        Policy = new PolicyService.Models.Policy
        {
            PolicyId = Guid.NewGuid(),
            Name = "Truck Cover",
            VehicleType = VehicleType.Truck,
            Premium = 5000,
            CoverageDetails = "Coverage",
            Terms = "Terms",
            PolicyDocument = "Doc"
        },
        VehicleNumber = "MH01AA1000",
        DrivingLicenseNumber = "LIC1000",
        StartDate = DateTime.UtcNow.Date.AddYears(-1),
        EndDate = endDate,
        Status = CustomerPolicyStatus.Active
    };

    private sealed class FakePolicyRepository : IPolicyRepository
    {
        public List<PolicyService.Models.Policy> Policies { get; } = [];
        public List<CustomerPolicy> CustomerPolicies { get; } = [];

        public Task AddPolicyAsync(PolicyService.Models.Policy policy, CancellationToken cancellationToken)
        {
            Policies.Add(policy);
            return Task.CompletedTask;
        }

        public Task<List<PolicyService.Models.Policy>> GetPoliciesAsync(CancellationToken cancellationToken) => Task.FromResult(Policies.ToList());
        public Task<PolicyService.Models.Policy?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken) => Task.FromResult(Policies.SingleOrDefault(x => x.PolicyId == policyId));
        public Task AddCustomerPolicyAsync(CustomerPolicy customerPolicy, CancellationToken cancellationToken)
        {
            if (customerPolicy.Policy is null)
            {
                customerPolicy.Policy = Policies.SingleOrDefault(x => x.PolicyId == customerPolicy.PolicyId);
            }
            CustomerPolicies.Add(customerPolicy);
            return Task.CompletedTask;
        }
        public Task<CustomerPolicy?> GetCustomerPolicyAsync(Guid customerPolicyId, CancellationToken cancellationToken) => Task.FromResult(CustomerPolicies.SingleOrDefault(x => x.CustomerPolicyId == customerPolicyId));
        public Task<List<CustomerPolicy>> GetCustomerPoliciesAsync(Guid customerId, CancellationToken cancellationToken) => Task.FromResult(CustomerPolicies.Where(x => x.CustomerId == customerId).ToList());
        public Task<List<CustomerPolicy>> GetPoliciesForReminderAsync(DateTime todayUtc, CancellationToken cancellationToken) => Task.FromResult(new List<CustomerPolicy>());
        public Task<List<CustomerPolicy>> GetPoliciesToExpireAsync(DateTime todayUtc, CancellationToken cancellationToken) => Task.FromResult(new List<CustomerPolicy>());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePolicyEventPublisher : IEventPublisher
    {
        public List<(string EventType, object Payload)> Published { get; } = [];
        public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            Published.Add((eventType, payload));
            return Task.CompletedTask;
        }
    }
}
