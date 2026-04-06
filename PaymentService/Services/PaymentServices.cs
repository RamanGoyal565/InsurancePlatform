using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;


namespace PaymentService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
    public sealed record PolicyPaymentRequest(Guid CustomerPolicyId, Guid CustomerId, Guid PolicyId, decimal Amount, string PaymentReference, string Operation);
    public sealed class PaymentService(IPaymentRepository repository, IEventPublisher eventPublisher) : IPaymentService
    {
        public async Task<Payment> ProcessAsync(CreatePaymentRequest request, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            if (!string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) && currentUser.UserId != request.CustomerId) throw new UnauthorizedAccessException("You can only pay for your own account.");
            var payment = new Payment
            {
                CustomerId = request.CustomerId,
                PolicyId = request.PolicyId,
                Amount = request.Amount,
                Source = "Manual",
                Status = PaymentStatus.Completed
            };

            await repository.AddAsync(payment, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PaymentCompleted", new { payment.PaymentId, payment.CustomerId, payment.PolicyId, payment.CustomerPolicyId, payment.Amount, payment.PaymentReference, payment.Source, Operation = "Manual" }, cancellationToken);
            return payment;
        }

        public async Task<Payment> ProcessPolicyPaymentAsync(PolicyPaymentRequest request, CancellationToken cancellationToken)
        {
            var payment = new Payment
            {
                CustomerId = request.CustomerId,
                PolicyId = request.PolicyId,
                CustomerPolicyId = request.CustomerPolicyId,
                PaymentReference = request.PaymentReference,
                Source = "PolicyWorkflow",
                Amount = request.Amount,
                Status = PaymentStatus.Completed
            };

            await repository.AddAsync(payment, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("PaymentCompleted", new { payment.PaymentId, payment.CustomerId, payment.PolicyId, payment.CustomerPolicyId, payment.Amount, payment.PaymentReference, payment.Source, request.Operation }, cancellationToken);
            return payment;
        }

        public Task<IReadOnlyList<Payment>> GetAsync(CancellationToken cancellationToken) => repository.GetAsync(cancellationToken).ContinueWith<IReadOnlyList<Payment>>(t => t.Result, cancellationToken);
    }
}