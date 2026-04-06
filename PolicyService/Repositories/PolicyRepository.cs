using Microsoft.EntityFrameworkCore;
using PolicyService.Data;
using PolicyService.Models;

namespace PolicyService.Repositories;
public sealed class PolicyRepository(PolicyDbContext dbContext) : IPolicyRepository
{
    public Task AddPolicyAsync(Policy policy, CancellationToken cancellationToken) => dbContext.Policies.AddAsync(policy, cancellationToken).AsTask();
    public Task<List<Policy>> GetPoliciesAsync(CancellationToken cancellationToken) => dbContext.Policies.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    public Task<Policy?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken) => dbContext.Policies.FirstOrDefaultAsync(x => x.PolicyId == policyId, cancellationToken);
    public Task AddCustomerPolicyAsync(CustomerPolicy customerPolicy, CancellationToken cancellationToken) => dbContext.CustomerPolicies.AddAsync(customerPolicy, cancellationToken).AsTask();
    public Task<CustomerPolicy?> GetCustomerPolicyAsync(Guid customerPolicyId, CancellationToken cancellationToken) => dbContext.CustomerPolicies.Include(x => x.Policy).FirstOrDefaultAsync(x => x.CustomerPolicyId == customerPolicyId, cancellationToken);
    public Task<List<CustomerPolicy>> GetCustomerPoliciesAsync(Guid customerId, CancellationToken cancellationToken) =>
        dbContext.CustomerPolicies
            .Include(x => x.Policy)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);
    public Task<List<CustomerPolicy>> GetPoliciesForReminderAsync(DateTime todayUtc, CancellationToken cancellationToken) =>
        dbContext.CustomerPolicies
            .Include(x => x.Policy)
            .Where(x => x.PendingOperation == null && x.Status != CustomerPolicyStatus.Cancelled && x.Status != CustomerPolicyStatus.Expired && x.EndDate.Date > todayUtc.Date && x.EndDate.Date <= todayUtc.Date.AddDays(30))
            .ToListAsync(cancellationToken);
    public Task<List<CustomerPolicy>> GetPoliciesToExpireAsync(DateTime todayUtc, CancellationToken cancellationToken) =>
        dbContext.CustomerPolicies
            .Include(x => x.Policy)
            .Where(x => x.PendingOperation == null && x.Status != CustomerPolicyStatus.Cancelled && x.Status != CustomerPolicyStatus.Expired && x.EndDate.Date < todayUtc.Date)
            .ToListAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}