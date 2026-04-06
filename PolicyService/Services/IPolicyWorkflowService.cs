using PolicyService.DTOs;
using PolicyService.Models;

namespace PolicyService.Services
{
    public interface IPolicyWorkflowService
    {
        Task<Policy> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken);
        Task<Policy> UpdatePolicyAsync(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<CustomerPolicyResponse>> GetCustomerPoliciesAsync(CurrentUser user, CancellationToken cancellationToken);
        Task<PolicyDocumentResponse> GetPolicyDocumentAsync(Guid policyId, CancellationToken cancellationToken);
        Task<TicketPolicyValidationResponse> ValidateTicketPolicyAsync(Guid policyId, Guid customerId, CancellationToken cancellationToken);
        Task<CustomerPolicy> PurchaseAsync(PurchasePolicyRequest request, CurrentUser user, CancellationToken cancellationToken);
        Task<CustomerPolicy> RenewAsync(Guid customerPolicyId, CurrentUser user, CancellationToken cancellationToken);
    }
}
