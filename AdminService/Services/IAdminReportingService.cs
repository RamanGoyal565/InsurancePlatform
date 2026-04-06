using AdminService.DTOs;

namespace AdminService.Services
{
    public interface IAdminReportingService
    {
        Task<DashboardSummaryResponse> GetDashboardAsync(CancellationToken cancellationToken);
        Task<TicketReportsResponse> GetTicketReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<ClaimReportsResponse> GetClaimReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<RevenueReportsResponse> GetRevenueReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<PolicyReportsResponse> GetPolicyReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<UserReportsResponse> GetUserReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<PerformanceReportsResponse> GetPerformanceReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
    }
}
