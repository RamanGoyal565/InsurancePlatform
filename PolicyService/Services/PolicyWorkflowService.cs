using PolicyService.DTOs;
using PolicyService.Models;
using PolicyService.Repositories;

namespace PolicyService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
    public sealed record PaymentRequestedMessage(Guid CustomerPolicyId, Guid CustomerId, Guid PolicyId, decimal Amount, string PaymentReference, string Operation);
    public sealed record PaymentCompletedMessage(Guid PaymentId, Guid CustomerId, Guid? PolicyId, Guid? CustomerPolicyId, decimal Amount, string PaymentReference, string Source, string Operation);
    public sealed class PolicyWorkflowService(IPolicyRepository repository, IEventPublisher eventPublisher) : IPolicyWorkflowService
    {
        public async Task<Policy> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken)
        {
            var policy = new Policy
            {
                Name = request.Name.Trim(),
                VehicleType = request.VehicleType,
                Premium = request.Premium,
                CoverageDetails = request.CoverageDetails.Trim(),
                Terms = request.Terms.Trim(),
                PolicyDocument = BuildPolicyDocument(request.Name, request.VehicleType, request.Premium, request.CoverageDetails, request.Terms, request.PolicyDocument)
            };

            await repository.AddPolicyAsync(policy, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PolicyCreated", new { policy.PolicyId, policy.Name, VehicleType = policy.VehicleType.ToString(), policy.Premium }, cancellationToken);
            return policy;
        }
        public async Task<Policy> UpdatePolicyAsync(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken)
        {
            var policy = await repository.GetPolicyAsync(policyId, cancellationToken) ?? throw new KeyNotFoundException("Policy not found.");
            policy.Name = request.Name.Trim();
            policy.VehicleType = request.VehicleType;
            policy.Premium = request.Premium;
            policy.CoverageDetails = request.CoverageDetails.Trim();
            policy.Terms = request.Terms.Trim();
            policy.PolicyDocument = BuildPolicyDocument(request.Name, request.VehicleType, request.Premium, request.CoverageDetails, request.Terms, request.PolicyDocument);
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PolicyUpdated", new { policy.PolicyId, policy.Name, VehicleType = policy.VehicleType.ToString(), policy.Premium }, cancellationToken);
            return policy;
        }
        public async Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken cancellationToken) => await repository.GetPoliciesAsync(cancellationToken);
        public async Task<IReadOnlyList<CustomerPolicyResponse>> GetCustomerPoliciesAsync(CurrentUser user, CancellationToken cancellationToken)
        {
            if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Only customers can view purchased policies.");
            var customerPolicies = await repository.GetCustomerPoliciesAsync(user.UserId, cancellationToken);
            return customerPolicies
                .Where(x => x.Policy is not null)
                .Select(x => new CustomerPolicyResponse(
                    x.CustomerPolicyId,
                    x.PolicyId,
                    x.Policy!.Name,
                    x.Policy.VehicleType,
                    x.Policy.Premium,
                    x.VehicleNumber,
                    x.DrivingLicenseNumber,
                    x.Status,
                    x.StartDate,
                    x.EndDate))
                .ToList();
        }
        public async Task<PolicyDocumentResponse> GetPolicyDocumentAsync(Guid policyId, CancellationToken cancellationToken)
        {
            var policy = await repository.GetPolicyAsync(policyId, cancellationToken) ?? throw new KeyNotFoundException("Policy not found.");
            return new PolicyDocumentResponse(policy.PolicyId, policy.Name, policy.PolicyDocument);
        }
        public async Task<TicketPolicyValidationResponse> ValidateTicketPolicyAsync(Guid policyId, Guid customerId, CancellationToken cancellationToken)
        {
            var policy = await repository.GetPolicyAsync(policyId, cancellationToken);
            if (policy is null)
            {
                return new TicketPolicyValidationResponse(false, false);
            }

            var customerPolicies = await repository.GetCustomerPoliciesAsync(customerId, cancellationToken);
            var customerOwnsPolicy = customerPolicies.Any(x => x.PolicyId == policyId && x.Status != CustomerPolicyStatus.Cancelled);
            return new TicketPolicyValidationResponse(true, customerOwnsPolicy);
        }
        public async Task<CustomerPolicy> PurchaseAsync(PurchasePolicyRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Only customers can purchase policies.");
            var policy = await repository.GetPolicyAsync(request.PolicyId, cancellationToken) ?? throw new KeyNotFoundException("Policy not found.");
            var start = request.StartDate?.Date ?? DateTime.UtcNow.Date;
            var customerPolicy = new CustomerPolicy
            {
                PolicyId = policy.PolicyId,
                CustomerId = user.UserId,
                VehicleNumber = request.VehicleNumber.Trim(),
                DrivingLicenseNumber = request.DrivingLicenseNumber.Trim(),
                PendingOperation = PolicyPaymentOperation.Purchase,
                StartDate = start,
                EndDate = start.AddYears(1),
                Status = CustomerPolicyStatus.Pending
            };

            await repository.AddCustomerPolicyAsync(customerPolicy, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PaymentRequested", new PaymentRequestedMessage(customerPolicy.CustomerPolicyId, customerPolicy.CustomerId, customerPolicy.PolicyId, policy.Premium, request.PaymentReference, PolicyPaymentOperation.Purchase.ToString()), cancellationToken);
            return customerPolicy;
        }
        public async Task<CustomerPolicy> RenewAsync(Guid customerPolicyId, CurrentUser user, CancellationToken cancellationToken)
        {
            var customerPolicy = await repository.GetCustomerPolicyAsync(customerPolicyId, cancellationToken) ?? throw new KeyNotFoundException("Customer policy not found.");
            if (customerPolicy.CustomerId != user.UserId) throw new UnauthorizedAccessException("You cannot renew this policy.");
            if (customerPolicy.PendingOperation is not null || customerPolicy.Status == CustomerPolicyStatus.Pending) throw new InvalidOperationException("A payment is already pending for this policy.");
            if (customerPolicy.Policy is null) throw new InvalidOperationException("Policy details were not loaded for renewal.");
            if (customerPolicy.EndDate.AddDays(15) < DateTime.UtcNow.Date) throw new InvalidOperationException("Policy alredy expired and past grace period");
            if (customerPolicy.EndDate.AddMonths(-1) > DateTime.UtcNow.Date) throw new InvalidOperationException("Policy can not br renewed before one month");
            customerPolicy.PendingOperation = PolicyPaymentOperation.Renewal;
            customerPolicy.Status = CustomerPolicyStatus.Pending;
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PaymentRequested", new PaymentRequestedMessage(customerPolicy.CustomerPolicyId, customerPolicy.CustomerId, customerPolicy.PolicyId, customerPolicy.Policy.Premium, $"RENEW-{customerPolicy.CustomerPolicyId}", PolicyPaymentOperation.Renewal.ToString()), cancellationToken);
            return customerPolicy;
        }
        private static string BuildPolicyDocument(string name, VehicleType vehicleType, decimal premium, string coverageDetails, string terms, string? requestedDocument)
        {
            if (!string.IsNullOrWhiteSpace(requestedDocument)) return requestedDocument.Trim();
            return $"Policy Name: {name.Trim()}\nPolicy Type: Vehicle\nVehicle Category: {vehicleType}\nPremium: {premium:F2}\n\nCoverage Details:\n{coverageDetails.Trim()}\n\nTerms:\n{terms.Trim()}";
        }
    }
}
