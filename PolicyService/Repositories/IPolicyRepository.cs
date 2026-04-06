using PolicyService.Models;

namespace PolicyService.Repositories
{
    public interface IPolicyRepository
    {
        Task AddPolicyAsync(Policy policy, CancellationToken cancellationToken);
        Task<List<Policy>> GetPoliciesAsync(CancellationToken cancellationToken);
        Task<Policy?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken);
        Task AddCustomerPolicyAsync(CustomerPolicy customerPolicy, CancellationToken cancellationToken);
        Task<CustomerPolicy?> GetCustomerPolicyAsync(Guid customerPolicyId, CancellationToken cancellationToken);
        Task<List<CustomerPolicy>> GetCustomerPoliciesAsync(Guid customerId, CancellationToken cancellationToken);
        Task<List<CustomerPolicy>> GetPoliciesForReminderAsync(DateTime todayUtc, CancellationToken cancellationToken);
        Task<List<CustomerPolicy>> GetPoliciesToExpireAsync(DateTime todayUtc, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
