using PolicyService.DTOs;
using PolicyService.Models;
using PolicyService.Repositories;

namespace PolicyService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
    public sealed record PaymentRequestedMessage(Guid CustomerPolicyId, Guid CustomerId, Guid PolicyId, decimal Amount, string PaymentReference, string Operation);
    public sealed record PaymentCompletedMessage(Guid PaymentId, Guid CustomerId, Guid? PolicyId, Guid? CustomerPolicyId, decimal Amount, string PaymentReference, string Source, string Operation);
    public sealed record PaymentFailedMessage(Guid PaymentId, Guid CustomerId, Guid? PolicyId, Guid? CustomerPolicyId, decimal Amount, string PaymentReference, string Source, string Operation, string Reason);

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
                PolicyDocument = string.Empty // generated after save so PolicyId is available
            };

            await repository.AddPolicyAsync(policy, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            // Generate PDF now that PolicyId exists
            policy.PolicyDocument = PolicyPdfGenerator.GenerateBase64(policy);
            await repository.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync("PolicyCreated", new
            {
                policy.PolicyId,
                policy.Name,
                VehicleType = policy.VehicleType.ToString(),
                policy.Premium
            }, cancellationToken);
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
            // Regenerate PDF with updated details
            policy.PolicyDocument = PolicyPdfGenerator.GenerateBase64(policy);

            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PolicyUpdated", new
            {
                policy.PolicyId,
                policy.Name,
                VehicleType = policy.VehicleType.ToString(),
                policy.Premium
            }, cancellationToken);
            return policy;
        }

        public async Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken cancellationToken)
            => await repository.GetPoliciesAsync(cancellationToken);

        public async Task<IReadOnlyList<CustomerPolicyResponse>> GetCustomerPoliciesAsync(CurrentUser user, CancellationToken cancellationToken)
        {
            if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only customers can view purchased policies.");
            }

            var customerPolicies = await repository.GetCustomerPoliciesAsync(user.UserId, cancellationToken);
            return customerPolicies.Where(x => x.Policy is not null)
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
                    x.EndDate,
                    x.LastPaymentFailureReason,
                    x.LastPaymentFailedOnUtc))
                .ToList();
        }

        public async Task<PolicyDocumentResponse> GetPolicyDocumentAsync(Guid policyId, CancellationToken cancellationToken)
        {
            var policy = await repository.GetPolicyAsync(policyId, cancellationToken) ?? throw new KeyNotFoundException("Policy not found.");

            // Always regenerate the PDF fresh so coverage details always reflect the
            // current semicolon-split format (avoids serving stale cached PDFs).
            var freshPdf = PolicyPdfGenerator.GenerateBase64(policy);

            // Persist only if the stored value has changed (avoids unnecessary DB writes)
            if (policy.PolicyDocument != freshPdf)
            {
                policy.PolicyDocument = freshPdf;
                await repository.SaveChangesAsync(cancellationToken);
            }

            return new PolicyDocumentResponse(policy.PolicyId, policy.Name, freshPdf);
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

            // ── Duplicate vehicle number check ────────────────────────────────
            // A customer cannot have two active/pending policies for the same vehicle number.
            var existingPolicies = await repository.GetCustomerPoliciesAsync(user.UserId, cancellationToken);
            var normalizedVehicleNumber = request.VehicleNumber.Trim().ToUpperInvariant();

            var duplicate = existingPolicies.FirstOrDefault(x =>
                string.Equals(x.VehicleNumber.Trim(), normalizedVehicleNumber, StringComparison.OrdinalIgnoreCase)
                && x.Status is CustomerPolicyStatus.Active
                           or CustomerPolicyStatus.Renewed
                           or CustomerPolicyStatus.Pending);

            if (duplicate is not null)
            {
                throw new InvalidOperationException(
                    $"You already have an active policy for vehicle number {normalizedVehicleNumber}. " +
                    "A vehicle cannot be covered by more than one active policy at a time.");
            }
            // ─────────────────────────────────────────────────────────────────

            // Detect Razorpay payment: reference starts with "pay_" (Razorpay payment ID format)
            var isRazorpayPayment = !string.IsNullOrWhiteSpace(request.PaymentReference)
                && request.PaymentReference.StartsWith("pay_", StringComparison.OrdinalIgnoreCase);

            var customerPolicy = new CustomerPolicy
            {
                PolicyId = policy.PolicyId,
                CustomerId = user.UserId,
                VehicleNumber = request.VehicleNumber.Trim(),
                DrivingLicenseNumber = request.DrivingLicenseNumber.Trim(),
                // For Razorpay: payment already done, activate immediately
                // For internal flow: set Pending and trigger PaymentRequested
                PendingOperation = isRazorpayPayment ? null : PolicyPaymentOperation.Purchase,
                Status = isRazorpayPayment ? CustomerPolicyStatus.Active : CustomerPolicyStatus.Pending,
                LastPaymentFailureReason = null,
                LastPaymentFailedOnUtc = null,
                StartDate = start,
                EndDate = start.AddYears(1),
            };

            await repository.AddCustomerPolicyAsync(customerPolicy, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            if (isRazorpayPayment)
            {
                // Payment already recorded by RazorpayController — just publish PolicyPurchased
                await eventPublisher.PublishAsync("PolicyPurchased", new
                {
                    customerPolicy.CustomerPolicyId,
                    customerPolicy.PolicyId,
                    customerPolicy.CustomerId,
                    customerPolicy.VehicleNumber,
                    customerPolicy.DrivingLicenseNumber,
                    PaymentId = (Guid?)null
                }, cancellationToken);
            }
            else
            {
                // Internal payment workflow
                await eventPublisher.PublishAsync("PaymentRequested", new PaymentRequestedMessage(
                    customerPolicy.CustomerPolicyId,
                    customerPolicy.CustomerId,
                    customerPolicy.PolicyId,
                    policy.Premium,
                    request.PaymentReference,
                    PolicyPaymentOperation.Purchase.ToString()), cancellationToken);
            }

            return customerPolicy;
        }

        public async Task<CustomerPolicy> RenewAsync(Guid customerPolicyId, CurrentUser user, CancellationToken cancellationToken)
        {
            var customerPolicy = await repository.GetCustomerPolicyAsync(customerPolicyId, cancellationToken) ?? throw new KeyNotFoundException("Customer policy not found.");
            if (customerPolicy.CustomerId != user.UserId)
                throw new UnauthorizedAccessException("You cannot renew this policy.");
            if (customerPolicy.PendingOperation is not null || customerPolicy.Status == CustomerPolicyStatus.Pending)
                throw new InvalidOperationException("A payment is already pending for this policy.");
            if (customerPolicy.Policy is null)
                throw new InvalidOperationException("Policy details were not loaded for renewal.");
            if (customerPolicy.EndDate.AddDays(15) < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Policy alredy expired and past grace period");
            if (customerPolicy.EndDate.AddMonths(-1) > DateTime.UtcNow.Date)
                throw new InvalidOperationException("Policy can not br renewed before one month");

            customerPolicy.PendingOperation = PolicyPaymentOperation.Renewal;
            customerPolicy.LastPaymentFailureReason = null;
            customerPolicy.LastPaymentFailedOnUtc = null;
            customerPolicy.Status = CustomerPolicyStatus.Pending;
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PaymentRequested", new PaymentRequestedMessage(customerPolicy.CustomerPolicyId, customerPolicy.CustomerId, customerPolicy.PolicyId, customerPolicy.Policy.Premium, $"RENEW-{customerPolicy.CustomerPolicyId}", PolicyPaymentOperation.Renewal.ToString()), cancellationToken);
            return customerPolicy;
        }

    }
}
