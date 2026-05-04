using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories
{ 
    public sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
    {
        public Task AddAsync(Payment payment, CancellationToken cancellationToken) => dbContext.Payments.AddAsync(payment, cancellationToken).AsTask();
        public Task<List<Payment>> GetAsync(CancellationToken cancellationToken) => dbContext.Payments.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        public Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) => dbContext.Payments.Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        public Task<bool> ExistsByReferenceAsync(string paymentReference, CancellationToken cancellationToken) => dbContext.Payments.AnyAsync(x => x.PaymentReference == paymentReference, cancellationToken);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}