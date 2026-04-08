namespace AdminService.DTOs;

public sealed record CountByLabel(string Label, int Count);
public sealed record RevenueByLabel(string Label, decimal Amount);
public sealed record TrendPoint(string Period, int Count);
public sealed record RevenueTrendPoint(string Period, decimal Amount);

public sealed record DateGroupedCountReport(
    IEnumerable<TrendPoint> Daily,
    IEnumerable<TrendPoint> Weekly,
    IEnumerable<TrendPoint> Monthly);

public sealed record DateGroupedRevenueReport(
    IEnumerable<RevenueTrendPoint> Daily,
    IEnumerable<RevenueTrendPoint> Monthly,
    IEnumerable<RevenueTrendPoint> Yearly);

public sealed record TicketReportsResponse(
    int TotalTickets,
    IEnumerable<CountByLabel> TicketsByStatus,
    IEnumerable<CountByLabel> TicketsByType,
    DateGroupedCountReport TicketsByDate,
    int OpenTickets,
    int ClosedTickets,
    double? AverageResolutionTimeHours);

public sealed record ClaimReportsResponse(
    int TotalClaims,
    IEnumerable<CountByLabel> ClaimsByStatus,
    double ClaimApprovalRate,
    double ClaimRejectionRate,
    double? AverageClaimProcessingTimeHours,
    IEnumerable<CountByLabel> ClaimsByPolicyType,
    DateGroupedCountReport ClaimsByDate);

public sealed record RevenueByCustomerItem(Guid CustomerId, string? CustomerName, decimal Amount);

public sealed record RevenueReportsResponse(
    decimal TotalRevenue,
    DateGroupedRevenueReport RevenueByDate,
    IEnumerable<RevenueByLabel> RevenueByPolicyType,
    IEnumerable<RevenueByCustomerItem> RevenuePerCustomer,
    IEnumerable<RevenueTrendPoint> RevenueTrends);

public sealed record PolicyReportsResponse(
    int TotalPoliciesSold,
    int ActivePolicies,
    int ExpiredPolicies,
    IEnumerable<CountByLabel> PoliciesByType,
    DateGroupedCountReport PoliciesByDate,
    double PolicyRenewalRate);

public sealed record PolicyCustomerItem(
    Guid CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    bool IsActive,
    DateTime FirstPurchasedAtUtc,
    int PoliciesBought,
    int RenewalCount,
    string LatestStatus);

public sealed record PolicyCustomersReportResponse(
    Guid PolicyId,
    string? PolicyName,
    string? VehicleType,
    int TotalCustomers,
    IEnumerable<PolicyCustomerItem> Customers);

public sealed record UsersByRoleReport(int Customers, int ClaimsSpecialists, int SupportSpecialists);

public sealed record UserReportsResponse(
    int TotalUsers,
    UsersByRoleReport UsersByRole,
    int ActiveUsers,
    int InactiveUsers,
    DateGroupedCountReport NewUserRegistrations,
    IEnumerable<TrendPoint> CustomerGrowthTrends);

public sealed record ClaimsSpecialistPerformanceItem(
    Guid UserId,
    string? Name,
    int ClaimsProcessed,
    double ApprovalRate,
    double? AverageProcessingTimeHours);

public sealed record SupportSpecialistPerformanceItem(
    Guid UserId,
    string? Name,
    int TicketsHandled,
    double? AverageResolutionTimeHours);

public sealed record PerformanceReportsResponse(
    IEnumerable<ClaimsSpecialistPerformanceItem> ClaimsSpecialists,
    IEnumerable<SupportSpecialistPerformanceItem> SupportSpecialists);

public sealed record DashboardSummaryResponse(
    int TotalTickets,
    int OpenTickets,
    int TotalClaims,
    int ApprovedClaims,
    decimal TotalRevenue,
    int ActivePolicies,
    int TotalUsers);
