using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Services
{
    public interface IPaymentService
    {
        Task<Payment> ProcessAsync(CreatePaymentRequest request, CurrentUser currentUser, CancellationToken cancellationToken);
        Task<Payment> ProcessPolicyPaymentAsync(PolicyPaymentRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<Payment>> GetAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<Payment>> GetMyAsync(Guid customerId, CancellationToken cancellationToken);
    }
}
