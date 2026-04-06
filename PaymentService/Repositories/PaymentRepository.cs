using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories
{ 
    public sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
    {
        public Task AddAsync(Payment payment, CancellationToken cancellationToken) => dbContext.Payments.AddAsync(payment, cancellationToken).AsTask();
        public Task<List<Payment>> GetAsync(CancellationToken cancellationToken) => dbContext.Payments.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    }
}