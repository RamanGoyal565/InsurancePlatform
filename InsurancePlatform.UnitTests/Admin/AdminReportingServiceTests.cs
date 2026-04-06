using AdminService.Models;
using AdminService.Repositories;
using AdminService.Services;
using Xunit;

using AdminReportingWorkflow = AdminService.Services.AdminReportingService;

namespace InsurancePlatform.UnitTests.Admin;

public sealed class AdminReportingServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ComputesCombinedSummary()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var supportTicketId = Guid.NewGuid();
        var claimTicketId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("UserRegistered", new { UserId = customerId, Name = "Satyam", Email = "satyam@test.com", Role = "Customer" }, DaysAgo(10)),
            Audit("PolicyCreated", new { PolicyId = policyId, Name = "Car Protect", VehicleType = "Car", Premium = 1200m }, DaysAgo(9)),
            Audit("TicketCreated", new { TicketId = supportTicketId, CustomerId = customerId, Type = "Support", PolicyId = policyId }, DaysAgo(4)),
            Audit("TicketUpdated", new { TicketId = supportTicketId, CustomerId = customerId, Status = "Open" }, DaysAgo(3)),
            Audit("TicketCreated", new { TicketId = claimTicketId, CustomerId = customerId, Type = "Claim", PolicyId = policyId }, DaysAgo(2)),
            Audit("ClaimApproved", new { TicketId = claimTicketId, CustomerId = customerId, ApprovalStatus = "Approved", ChangedBy = Guid.NewGuid() }, DaysAgo(1)),
            Audit("PaymentCompleted", new { PaymentId = Guid.NewGuid(), CustomerId = customerId, PolicyId = policyId, Amount = 1200m }, DaysAgo(2)),
            Audit("PolicyPurchased", new { CustomerPolicyId = Guid.NewGuid(), PolicyId = policyId, CustomerId = customerId }, DaysAgo(2))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetDashboardAsync(CancellationToken.None);

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.TotalClaims);
        Assert.Equal(1, result.ApprovedClaims);
        Assert.Equal(1200m, result.TotalRevenue);
        Assert.Equal(1, result.ActivePolicies);
        Assert.Equal(1, result.TotalUsers);
    }

    [Fact]
    public async Task GetTicketReportsAsync_ComputesStatusesTypesAndResolutionTime()
    {
        var supportTicketId = Guid.NewGuid();
        var claimTicketId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var assignedTo = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("TicketCreated", new { TicketId = supportTicketId, CustomerId = customerId, Type = "Support" }, DaysAgo(5)),
            Audit("TicketAssigned", new { TicketId = supportTicketId, CustomerId = customerId, Type = "Support", AssignedTo = assignedTo }, DaysAgo(4)),
            Audit("TicketUpdated", new { TicketId = supportTicketId, CustomerId = customerId, Status = "Resolved", AssignedTo = assignedTo }, DaysAgo(3)),
            Audit("TicketCreated", new { TicketId = claimTicketId, CustomerId = customerId, Type = "Claim" }, DaysAgo(2))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetTicketReportsAsync(null, null, CancellationToken.None);

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.ClosedTickets);
        Assert.Equal(1, result.OpenTickets);
        Assert.Contains(result.TicketsByType, x => x.Label == "Support" && x.Count == 1);
        Assert.Contains(result.TicketsByType, x => x.Label == "Claim" && x.Count == 1);
        Assert.NotNull(result.AverageResolutionTimeHours);
    }

    [Fact]
    public async Task GetClaimReportsAsync_ComputesApprovalAndRejectionRates()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var approvedTicketId = Guid.NewGuid();
        var rejectedTicketId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("PolicyCreated", new { PolicyId = policyId, Name = "Car Protect", VehicleType = "Car", Premium = 1200m }, DaysAgo(10)),
            Audit("TicketCreated", new { TicketId = approvedTicketId, CustomerId = customerId, Type = "Claim", PolicyId = policyId }, DaysAgo(6)),
            Audit("ClaimApproved", new { TicketId = approvedTicketId, CustomerId = customerId, ApprovalStatus = "Approved", ChangedBy = Guid.NewGuid() }, DaysAgo(4)),
            Audit("TicketCreated", new { TicketId = rejectedTicketId, CustomerId = customerId, Type = "Claim", PolicyId = policyId }, DaysAgo(5)),
            Audit("ClaimRejected", new { TicketId = rejectedTicketId, CustomerId = customerId, ApprovalStatus = "Rejected", ChangedBy = Guid.NewGuid() }, DaysAgo(3))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetClaimReportsAsync(null, null, CancellationToken.None);

        Assert.Equal(2, result.TotalClaims);
        Assert.Equal(50, result.ClaimApprovalRate);
        Assert.Equal(50, result.ClaimRejectionRate);
        Assert.Contains(result.ClaimsByPolicyType, x => x.Label == "Car" && x.Count == 2);
        Assert.NotNull(result.AverageClaimProcessingTimeHours);
    }

    [Fact]
    public async Task GetRevenueReportsAsync_GroupsRevenueByPolicyTypeAndCustomer()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("UserRegistered", new { UserId = customerId, Name = "Satyam", Email = "satyam@test.com", Role = "Customer" }, DaysAgo(10)),
            Audit("PolicyCreated", new { PolicyId = policyId, Name = "Bike Cover", VehicleType = "Bike", Premium = 999m }, DaysAgo(9)),
            Audit("PaymentCompleted", new { PaymentId = Guid.NewGuid(), CustomerId = customerId, PolicyId = policyId, Amount = 999m }, DaysAgo(5)),
            Audit("PaymentCompleted", new { PaymentId = Guid.NewGuid(), CustomerId = customerId, PolicyId = policyId, Amount = 500m }, DaysAgo(4))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetRevenueReportsAsync(null, null, CancellationToken.None);

        Assert.Equal(1499m, result.TotalRevenue);
        Assert.Equal(1499m, result.RevenueByPolicyType.Single().Amount);
        Assert.Equal(customerId, result.RevenuePerCustomer.Single().CustomerId);
    }

    [Fact]
    public async Task GetPolicyReportsAsync_ComputesRenewalRateAndPolicyTypes()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("PolicyCreated", new { PolicyId = policyId, Name = "Truck Cover", VehicleType = "Truck", Premium = 2000m }, DaysAgo(12)),
            Audit("PolicyPurchased", new { CustomerPolicyId = Guid.NewGuid(), PolicyId = policyId, CustomerId = customerId }, DaysAgo(10)),
            Audit("PolicyRenewed", new { CustomerPolicyId = Guid.NewGuid(), PolicyId = policyId, CustomerId = customerId }, DaysAgo(2)),
            Audit("PolicyExpired", new { CustomerPolicyId = Guid.NewGuid(), PolicyId = policyId, CustomerId = customerId }, DaysAgo(1))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetPolicyReportsAsync(null, null, CancellationToken.None);

        Assert.Equal(3, result.TotalPoliciesSold);
        Assert.Equal(2, result.ActivePolicies);
        Assert.Equal(1, result.ExpiredPolicies);
        Assert.Contains(result.PoliciesByType, x => x.Label == "Truck");
        Assert.True(result.PolicyRenewalRate > 0);
    }

    [Fact]
    public async Task GetUserReportsAsync_ReturnsRoleAndActivityCounts()
    {
        var customerId = Guid.NewGuid();
        var claimsId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("UserRegistered", new { UserId = customerId, Name = "Customer", Email = "customer@test.com", Role = "Customer" }, DaysAgo(10)),
            Audit("UserRegistered", new { UserId = claimsId, Name = "Claim User", Email = "claims@test.com", Role = "ClaimsSpecialist" }, DaysAgo(9)),
            Audit("UserStatusUpdated", new { UserId = claimsId, Name = "Claim User", Email = "claims@test.com", Role = "ClaimsSpecialist", IsActive = false }, DaysAgo(2))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetUserReportsAsync(null, null, CancellationToken.None);

        Assert.Equal(2, result.TotalUsers);
        Assert.Equal(1, result.UsersByRole.Customers);
        Assert.Equal(1, result.UsersByRole.ClaimsSpecialists);
        Assert.Equal(1, result.ActiveUsers);
        Assert.Equal(1, result.InactiveUsers);
        Assert.NotEmpty(result.CustomerGrowthTrends);
    }

    [Fact]
    public async Task GetPerformanceReportsAsync_ComputesClaimsAndSupportMetrics()
    {
        var customerId = Guid.NewGuid();
        var claimsSpecialistId = Guid.NewGuid();
        var supportSpecialistId = Guid.NewGuid();
        var claimTicketId = Guid.NewGuid();
        var supportTicketId = Guid.NewGuid();
        var repository = new FakeAdminReadRepository(
        [
            Audit("UserRegistered", new { UserId = claimsSpecialistId, Name = "Claim Specialist", Email = "claims@test.com", Role = "ClaimsSpecialist" }, DaysAgo(15)),
            Audit("UserRegistered", new { UserId = supportSpecialistId, Name = "Support Specialist", Email = "support@test.com", Role = "SupportSpecialist" }, DaysAgo(15)),
            Audit("TicketCreated", new { TicketId = claimTicketId, CustomerId = customerId, Type = "Claim" }, DaysAgo(8)),
            Audit("ClaimApproved", new { TicketId = claimTicketId, CustomerId = customerId, ApprovalStatus = "Approved", ChangedBy = claimsSpecialistId }, DaysAgo(5)),
            Audit("TicketCreated", new { TicketId = supportTicketId, CustomerId = customerId, Type = "Support" }, DaysAgo(6)),
            Audit("TicketAssigned", new { TicketId = supportTicketId, CustomerId = customerId, Type = "Support", AssignedTo = supportSpecialistId }, DaysAgo(5)),
            Audit("TicketUpdated", new { TicketId = supportTicketId, CustomerId = customerId, Status = "Resolved", AssignedTo = supportSpecialistId }, DaysAgo(3))
        ]);
        var sut = new AdminReportingWorkflow(repository);

        var result = await sut.GetPerformanceReportsAsync(null, null, CancellationToken.None);

        Assert.Single(result.ClaimsSpecialists);
        Assert.Equal(1, result.ClaimsSpecialists.Single().ClaimsProcessed);
        Assert.Equal(100, result.ClaimsSpecialists.Single().ApprovalRate);
        Assert.Single(result.SupportSpecialists);
        Assert.Equal(1, result.SupportSpecialists.Single().TicketsHandled);
        Assert.NotNull(result.SupportSpecialists.Single().AverageResolutionTimeHours);
    }

    private static EventAudit Audit(string eventType, object payload, DateTime occurredOnUtc) => new()
    {
        EventType = eventType,
        Payload = System.Text.Json.JsonSerializer.Serialize(payload),
        OccurredOnUtc = occurredOnUtc
    };

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.Date.AddDays(-days);

    private sealed class FakeAdminReadRepository(IEnumerable<EventAudit> audits) : IAdminReadRepository
    {
        private readonly List<EventAudit> _audits = audits.ToList();
        public Task AddAsync(EventAudit audit, CancellationToken cancellationToken)
        {
            _audits.Add(audit);
            return Task.CompletedTask;
        }
        public Task<List<EventAudit>> GetAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            IEnumerable<EventAudit> query = _audits;
            if (fromUtc.HasValue) query = query.Where(x => x.OccurredOnUtc >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.OccurredOnUtc <= toUtc.Value);
            return Task.FromResult(query.OrderByDescending(x => x.OccurredOnUtc).ToList());
        }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}




