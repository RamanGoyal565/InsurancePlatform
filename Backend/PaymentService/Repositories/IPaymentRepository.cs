using PaymentService.Models;

namespace PaymentService.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment, CancellationToken cancellationToken);
        Task<List<Payment>> GetAsync(CancellationToken cancellationToken);
        Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
        Task<bool> ExistsByReferenceAsync(string paymentReference, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
