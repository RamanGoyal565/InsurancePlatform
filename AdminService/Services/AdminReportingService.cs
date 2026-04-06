using System.Globalization;
using System.Text.Json;
using AdminService.DTOs;
using AdminService.Models;
using AdminService.Repositories;

namespace AdminService.Services
{
    public sealed class AdminReportingService(IAdminReadRepository repository) : IAdminReportingService
    {
        public async Task<DashboardSummaryResponse> GetDashboardAsync(CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(null, null, cancellationToken);
            var tickets = BuildTicketSnapshots(audits);
            var claims = tickets.Where(x => x.Type == "Claim").ToList();
            var revenues = BuildPayments(audits);
            var policies = BuildCustomerPolicies(audits);
            var users = BuildUserSnapshots(audits);

            return new DashboardSummaryResponse(
                tickets.Count,
                tickets.Count(x => x.Status is "Open" or "Assigned" or "InReview"),
                claims.Count,
                claims.Count(x => x.ApprovalStatus == "Approved"),
                revenues.Sum(x => x.Amount),
                policies.Count(x => x.Status == "Active" || x.Status == "Renewed"),
                users.Count);
        }

        public async Task<TicketReportsResponse> GetTicketReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var tickets = BuildTicketSnapshots(audits);

            return new TicketReportsResponse(
                tickets.Count,
                BuildCounts(tickets.Select(x => x.Status)),
                BuildCounts(tickets.Select(x => x.Type)),
                BuildCountDateGroups(tickets.Select(x => x.CreatedAtUtc)),
                tickets.Count(x => x.Status is "Open" or "Assigned" or "InReview"),
                tickets.Count(x => x.Status is "Resolved" or "Closed"),
                AverageHours(tickets.Where(x => x.ResolvedAtUtc.HasValue).Select(x => x.ResolvedAtUtc!.Value - x.CreatedAtUtc)));
        }

        public async Task<ClaimReportsResponse> GetClaimReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var claims = BuildTicketSnapshots(audits).Where(x => x.Type == "Claim").ToList();
            var policyDefinitions = BuildPolicyDefinitions(audits);

            var approved = claims.Count(x => x.ApprovalStatus == "Approved");
            var rejected = claims.Count(x => x.ApprovalStatus == "Rejected");
            var total = claims.Count;

            return new ClaimReportsResponse(
                total,
                BuildCounts(claims.Select(x => x.ApprovalStatus ?? "Pending")),
                Rate(approved, total),
                Rate(rejected, total),
                AverageHours(claims.Where(x => x.DecidedAtUtc.HasValue).Select(x => x.DecidedAtUtc!.Value - x.CreatedAtUtc)),
                BuildCounts(claims.Select(x => ResolvePolicyType(x.PolicyId, policyDefinitions))),
                BuildCountDateGroups(claims.Select(x => x.CreatedAtUtc)));
        }

        public async Task<RevenueReportsResponse> GetRevenueReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var payments = BuildPayments(audits);
            var users = BuildUserSnapshots(audits).ToDictionary(x => x.UserId, x => x);
            var policyDefinitions = BuildPolicyDefinitions(audits);

            return new RevenueReportsResponse(
                payments.Sum(x => x.Amount),
                BuildRevenueDateGroups(payments),
                payments.Where(x => x.PolicyId.HasValue)
                    .GroupBy(x => ResolvePolicyType(x.PolicyId, policyDefinitions))
                    .OrderBy(x => x.Key)
                    .Select(x => new RevenueByLabel(x.Key, x.Sum(y => y.Amount)))
                    .ToList(),
                payments.GroupBy(x => x.CustomerId)
                    .OrderByDescending(x => x.Sum(y => y.Amount))
                    .Select(x => new RevenueByCustomerItem(x.Key, users.TryGetValue(x.Key, out var user) ? user.Name : null, x.Sum(y => y.Amount)))
                    .ToList(),
                GroupRevenueByMonth(payments));
        }

        public async Task<PolicyReportsResponse> GetPolicyReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var policies = BuildCustomerPolicies(audits);
            var policyDefinitions = BuildPolicyDefinitions(audits);
            var sold = policies.Count;
            var renewals = policies.Sum(x => x.RenewalCount);

            return new PolicyReportsResponse(
                sold,
                policies.Count(x => x.Status is "Active" or "Renewed"),
                policies.Count(x => x.Status == "Expired"),
                BuildCounts(policies.Select(x => ResolvePolicyType(x.PolicyId, policyDefinitions))),
                BuildCountDateGroups(policies.Select(x => x.PurchasedAtUtc)),
                Rate(renewals, sold));
        }

        public async Task<UserReportsResponse> GetUserReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var users = BuildUserSnapshots(audits);
            var registrations = audits.Where(x => x.EventType == "UserRegistered").Select(x => x.OccurredOnUtc).ToList();
            var customers = users.Where(x => x.Role == "Customer").OrderBy(x => x.RegisteredOnUtc).ToList();

            return new UserReportsResponse(
                users.Count,
                new UsersByRoleReport(
                    users.Count(x => x.Role == "Customer"),
                    users.Count(x => x.Role == "ClaimsSpecialist"),
                    users.Count(x => x.Role == "SupportSpecialist")),
                users.Count(x => x.IsActive),
                users.Count(x => !x.IsActive),
                BuildCountDateGroups(registrations),
                BuildCumulativeCustomerGrowth(customers));
        }

        public async Task<PerformanceReportsResponse> GetPerformanceReportsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            var audits = await repository.GetAsync(fromUtc, toUtc, cancellationToken);
            var users = BuildUserSnapshots(audits).ToDictionary(x => x.UserId, x => x);
            var tickets = BuildTicketSnapshots(audits);
            var claims = tickets.Where(x => x.Type == "Claim" && x.ProcessedBy.HasValue).ToList();
            var supportTickets = tickets.Where(x => x.Type == "Support" && x.AssignedTo.HasValue).ToList();

            var claimsSpecialists = claims
                .GroupBy(x => x.ProcessedBy!.Value)
                .Select(x => new ClaimsSpecialistPerformanceItem(
                    x.Key,
                    users.TryGetValue(x.Key, out var user) ? user.Name : null,
                    x.Count(),
                    Rate(x.Count(y => y.ApprovalStatus == "Approved"), x.Count()),
                    AverageHours(x.Where(y => y.DecidedAtUtc.HasValue).Select(y => y.DecidedAtUtc!.Value - y.CreatedAtUtc))))
                .OrderByDescending(x => x.ClaimsProcessed)
                .ToList();

            var supportSpecialists = supportTickets
                .GroupBy(x => x.AssignedTo!.Value)
                .Select(x => new SupportSpecialistPerformanceItem(
                    x.Key,
                    users.TryGetValue(x.Key, out var user) ? user.Name : null,
                    x.Count(),
                    AverageHours(x.Where(y => y.ResolvedAtUtc.HasValue).Select(y => y.ResolvedAtUtc!.Value - y.CreatedAtUtc))))
                .OrderByDescending(x => x.TicketsHandled)
                .ToList();

            return new PerformanceReportsResponse(claimsSpecialists, supportSpecialists);
        }

        private static List<UserSnapshot> BuildUserSnapshots(IReadOnlyList<EventAudit> audits)
        {
            var users = new Dictionary<Guid, UserSnapshot>();
            foreach (var audit in audits.OrderBy(x => x.OccurredOnUtc))
            {
                if (!TryParsePayload(audit.Payload, out var payload))
                {
                    continue;
                }

                if (audit.EventType == "UserRegistered")
                {
                    var userId = GetGuid(payload, "UserId");
                    if (userId is null) continue;
                    users[userId.Value] = new UserSnapshot(userId.Value, GetString(payload, "Name"), GetString(payload, "Email"), GetString(payload, "Role") ?? "Unknown", true, audit.OccurredOnUtc);
                }
                else if (audit.EventType == "UserStatusUpdated")
                {
                    var userId = GetGuid(payload, "UserId");
                    if (userId is null) continue;
                    var existing = users.TryGetValue(userId.Value, out var snapshot)
                        ? snapshot
                        : new UserSnapshot(userId.Value, GetString(payload, "Name"), GetString(payload, "Email"), GetString(payload, "Role") ?? "Unknown", true, audit.OccurredOnUtc);
                    users[userId.Value] = existing with
                    {
                        Name = GetString(payload, "Name") ?? existing.Name,
                        Email = GetString(payload, "Email") ?? existing.Email,
                        Role = GetString(payload, "Role") ?? existing.Role,
                        IsActive = GetBool(payload, "IsActive") ?? existing.IsActive
                    };
                }
            }

            return users.Values.ToList();
        }

        private static Dictionary<Guid, PolicyDefinitionSnapshot> BuildPolicyDefinitions(IReadOnlyList<EventAudit> audits)
        {
            var policies = new Dictionary<Guid, PolicyDefinitionSnapshot>();
            foreach (var audit in audits.Where(x => x.EventType is "PolicyCreated" or "PolicyUpdated").OrderBy(x => x.OccurredOnUtc))
            {
                if (!TryParsePayload(audit.Payload, out var payload)) continue;
                var policyId = GetGuid(payload, "PolicyId");
                if (policyId is null) continue;
                policies[policyId.Value] = new PolicyDefinitionSnapshot(policyId.Value, GetString(payload, "Name"), GetString(payload, "VehicleType"), GetDecimal(payload, "Premium") ?? 0m);
            }
            return policies;
        }

        private static List<CustomerPolicySnapshot> BuildCustomerPolicies(IReadOnlyList<EventAudit> audits)
        {
            var customerPolicies = new Dictionary<Guid, CustomerPolicySnapshot>();
            foreach (var audit in audits.OrderBy(x => x.OccurredOnUtc))
            {
                if (!TryParsePayload(audit.Payload, out var payload)) continue;
                var customerPolicyId = GetGuid(payload, "CustomerPolicyId");
                if (customerPolicyId is null) continue;

                var existing = customerPolicies.TryGetValue(customerPolicyId.Value, out var snapshot)
                    ? snapshot
                    : new CustomerPolicySnapshot(customerPolicyId.Value, GetGuid(payload, "PolicyId") ?? Guid.Empty, GetGuid(payload, "CustomerId") ?? Guid.Empty, "Unknown", audit.OccurredOnUtc, 0);

                switch (audit.EventType)
                {
                    case "PolicyPurchased":
                        existing = existing with
                        {
                            PolicyId = GetGuid(payload, "PolicyId") ?? existing.PolicyId,
                            CustomerId = GetGuid(payload, "CustomerId") ?? existing.CustomerId,
                            Status = "Active",
                            PurchasedAtUtc = audit.OccurredOnUtc
                        };
                        break;
                    case "PolicyRenewed":
                        existing = existing with
                        {
                            Status = "Renewed",
                            RenewalCount = existing.RenewalCount + 1
                        };
                        break;
                    case "PolicyExpired":
                        existing = existing with { Status = "Expired" };
                        break;
                }

                customerPolicies[customerPolicyId.Value] = existing;
            }

            return customerPolicies.Values.Where(x => x.PolicyId != Guid.Empty).ToList();
        }

        private static List<PaymentSnapshot> BuildPayments(IReadOnlyList<EventAudit> audits)
        {
            var payments = new List<PaymentSnapshot>();
            foreach (var audit in audits.Where(x => x.EventType == "PaymentCompleted"))
            {
                if (!TryParsePayload(audit.Payload, out var payload)) continue;
                var paymentId = GetGuid(payload, "PaymentId");
                var customerId = GetGuid(payload, "CustomerId");
                var amount = GetDecimal(payload, "Amount");
                if (paymentId is null || customerId is null || amount is null) continue;
                payments.Add(new PaymentSnapshot(paymentId.Value, customerId.Value, GetGuid(payload, "PolicyId"), GetGuid(payload, "CustomerPolicyId"), amount.Value, audit.OccurredOnUtc));
            }
            return payments;
        }

        private static List<TicketSnapshot> BuildTicketSnapshots(IReadOnlyList<EventAudit> audits)
        {
            var tickets = new Dictionary<Guid, TicketSnapshot>();
            foreach (var audit in audits.OrderBy(x => x.OccurredOnUtc))
            {
                if (!TryParsePayload(audit.Payload, out var payload)) continue;
                var ticketId = GetGuid(payload, "TicketId");
                if (ticketId is null) continue;

                var existing = tickets.TryGetValue(ticketId.Value, out var snapshot)
                    ? snapshot
                    : new TicketSnapshot(ticketId.Value, GetString(payload, "Type") ?? "Unknown", "Unknown", GetGuid(payload, "CustomerId") ?? Guid.Empty, GetGuid(payload, "PolicyId"), null, audit.OccurredOnUtc, null, null, null, null, null, 0);

                switch (audit.EventType)
                {
                    case "TicketCreated":
                        existing = existing with
                        {
                            Type = GetString(payload, "Type") ?? existing.Type,
                            Status = "Open",
                            CustomerId = GetGuid(payload, "CustomerId") ?? existing.CustomerId,
                            PolicyId = GetGuid(payload, "PolicyId") ?? existing.PolicyId,
                            CreatedAtUtc = audit.OccurredOnUtc
                        };
                        break;
                    case "TicketAssigned":
                        existing = existing with
                        {
                            Type = GetString(payload, "Type") ?? existing.Type,
                            AssignedTo = GetGuid(payload, "AssignedTo") ?? existing.AssignedTo,
                            Status = "Assigned"
                        };
                        break;
                    case "TicketUpdated":
                        var updatedStatus = GetString(payload, "Status") ?? existing.Status;
                        existing = existing with
                        {
                            AssignedTo = GetGuid(payload, "AssignedTo") ?? existing.AssignedTo,
                            Status = updatedStatus,
                            ResolvedAtUtc = updatedStatus is "Resolved" or "Closed" ? audit.OccurredOnUtc : existing.ResolvedAtUtc
                        };
                        break;
                    case "CommentAdded":
                        existing = existing with
                        {
                            AssignedTo = GetGuid(payload, "AssignedTo") ?? existing.AssignedTo,
                            CommentCount = existing.CommentCount + 1
                        };
                        break;
                    case "ClaimApproved":
                    case "ClaimRejected":
                        existing = existing with
                        {
                            AssignedTo = GetGuid(payload, "AssignedTo") ?? existing.AssignedTo,
                            Status = audit.EventType == "ClaimApproved" ? "Resolved" : "Rejected",
                            ApprovalStatus = GetString(payload, "ApprovalStatus") ?? (audit.EventType == "ClaimApproved" ? "Approved" : "Rejected"),
                            ProcessedBy = GetGuid(payload, "ChangedBy") ?? existing.ProcessedBy,
                            DecidedAtUtc = audit.OccurredOnUtc,
                            ResolvedAtUtc = audit.OccurredOnUtc
                        };
                        break;
                }

                tickets[ticketId.Value] = existing;
            }

            return tickets.Values.Where(x => x.CustomerId != Guid.Empty).ToList();
        }

        private static IEnumerable<CountByLabel> BuildCounts(IEnumerable<string> labels)
            => labels.Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x)
                .OrderBy(x => x.Key)
                .Select(x => new CountByLabel(x.Key, x.Count()))
                .ToList();

        private static DateGroupedCountReport BuildCountDateGroups(IEnumerable<DateTime> dates)
        {
            var dateList = dates.Select(x => x.Date).ToList();
            return new DateGroupedCountReport(
                dateList.GroupBy(x => x.ToString("yyyy-MM-dd")).OrderBy(x => x.Key).Select(x => new TrendPoint(x.Key, x.Count())).ToList(),
                dateList.GroupBy(StartOfWeek).OrderBy(x => x.Key).Select(x => new TrendPoint(x.Key.ToString("yyyy-MM-dd"), x.Count())).ToList(),
                dateList.GroupBy(x => new DateTime(x.Year, x.Month, 1)).OrderBy(x => x.Key).Select(x => new TrendPoint(x.Key.ToString("yyyy-MM"), x.Count())).ToList());
        }

        private static DateGroupedRevenueReport BuildRevenueDateGroups(IEnumerable<PaymentSnapshot> payments)
        {
            var paymentList = payments.ToList();
            return new DateGroupedRevenueReport(
                paymentList.GroupBy(x => x.OccurredOnUtc.Date).OrderBy(x => x.Key).Select(x => new RevenueTrendPoint(x.Key.ToString("yyyy-MM-dd"), x.Sum(y => y.Amount))).ToList(),
                paymentList.GroupBy(x => new DateTime(x.OccurredOnUtc.Year, x.OccurredOnUtc.Month, 1)).OrderBy(x => x.Key).Select(x => new RevenueTrendPoint(x.Key.ToString("yyyy-MM"), x.Sum(y => y.Amount))).ToList(),
                paymentList.GroupBy(x => new DateTime(x.OccurredOnUtc.Year, 1, 1)).OrderBy(x => x.Key).Select(x => new RevenueTrendPoint(x.Key.ToString("yyyy"), x.Sum(y => y.Amount))).ToList());
        }

        private static IEnumerable<RevenueTrendPoint> GroupRevenueByMonth(IEnumerable<PaymentSnapshot> payments)
            => payments.GroupBy(x => new DateTime(x.OccurredOnUtc.Year, x.OccurredOnUtc.Month, 1))
                .OrderBy(x => x.Key)
                .Select(x => new RevenueTrendPoint(x.Key.ToString("yyyy-MM"), x.Sum(y => y.Amount)))
                .ToList();

        private static string ResolvePolicyType(Guid? policyId, IReadOnlyDictionary<Guid, PolicyDefinitionSnapshot> policies)
        {
            if (!policyId.HasValue)
            {
                return "Unknown";
            }

            return policies.TryGetValue(policyId.Value, out var policy) && !string.IsNullOrWhiteSpace(policy.VehicleType)
                ? policy.VehicleType!
                : "Unknown";
        }

        private static double Rate(int numerator, int denominator)
            => denominator == 0 ? 0 : Math.Round((double)numerator * 100 / denominator, 2);

        private static double? AverageHours(IEnumerable<TimeSpan> spans)
        {
            var values = spans.Select(x => x.TotalHours).ToList();
            return values.Count == 0 ? null : Math.Round(values.Average(), 2);
        }

        private static IEnumerable<TrendPoint> BuildCumulativeCustomerGrowth(IEnumerable<UserSnapshot> customers)
        {
            var points = new List<TrendPoint>();
            var runningTotal = 0;
            foreach (var group in customers.GroupBy(x => new DateTime(x.RegisteredOnUtc.Year, x.RegisteredOnUtc.Month, 1)).OrderBy(x => x.Key))
            {
                runningTotal += group.Count();
                points.Add(new TrendPoint(group.Key.ToString("yyyy-MM"), runningTotal));
            }
            return points;
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private static bool TryParsePayload(string payload, out JsonElement root)
        {
            try
            {
                root = JsonDocument.Parse(payload).RootElement.Clone();
                return true;
            }
            catch
            {
                root = default;
                return false;
            }
        }

        private static string? GetString(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static Guid? GetGuid(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value)) return null;
            return value.ValueKind switch
            {
                JsonValueKind.String when Guid.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static bool? GetBool(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value)) return null;
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        private static decimal? GetDecimal(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value)) return null;
            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetDecimal(out var parsed) => parsed,
                JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };
        }

        private sealed record UserSnapshot(Guid UserId, string? Name, string? Email, string Role, bool IsActive, DateTime RegisteredOnUtc);
        private sealed record PolicyDefinitionSnapshot(Guid PolicyId, string? Name, string? VehicleType, decimal Premium);
        private sealed record CustomerPolicySnapshot(Guid CustomerPolicyId, Guid PolicyId, Guid CustomerId, string Status, DateTime PurchasedAtUtc, int RenewalCount);
        private sealed record PaymentSnapshot(Guid PaymentId, Guid CustomerId, Guid? PolicyId, Guid? CustomerPolicyId, decimal Amount, DateTime OccurredOnUtc);
        private sealed record TicketSnapshot(Guid TicketId, string Type, string Status, Guid CustomerId, Guid? PolicyId, Guid? AssignedTo, DateTime CreatedAtUtc, DateTime? ResolvedAtUtc, DateTime? FirstResponseAtUtc, string? ApprovalStatus, Guid? ProcessedBy, DateTime? DecidedAtUtc, int CommentCount);
    }
}
