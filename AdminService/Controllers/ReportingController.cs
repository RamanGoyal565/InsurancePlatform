using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("")]
public sealed class ReportingController(IAdminReportingService reportingService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard(CancellationToken cancellationToken) => Ok(await reportingService.GetDashboardAsync(cancellationToken));

    [HttpGet("reports/tickets")]
    public async Task<ActionResult> TicketReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetTicketReportsAsync(fromUtc, toUtc, cancellationToken));

    [HttpGet("reports/claims")]
    public async Task<ActionResult> ClaimReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetClaimReportsAsync(fromUtc, toUtc, cancellationToken));

    [HttpGet("reports/revenue")]
    public async Task<ActionResult> RevenueReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetRevenueReportsAsync(fromUtc, toUtc, cancellationToken));

    [HttpGet("reports/policies")]
    public async Task<ActionResult> PolicyReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetPolicyReportsAsync(fromUtc, toUtc, cancellationToken));

    [HttpGet("reports/users")]
    public async Task<ActionResult> UserReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetUserReportsAsync(fromUtc, toUtc, cancellationToken));

    [HttpGet("reports/performance")]
    public async Task<ActionResult> PerformanceReports([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
        => Ok(await reportingService.GetPerformanceReportsAsync(fromUtc, toUtc, cancellationToken));
}
