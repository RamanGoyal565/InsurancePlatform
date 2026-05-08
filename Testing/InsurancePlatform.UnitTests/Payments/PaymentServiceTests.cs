using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Services;
using Xunit;

using PaymentWorkflow = PaymentService.Services.PaymentService;

namespace InsurancePlatform.UnitTests.Payments;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task ProcessAsync_ThrowsWhenUserPaysForAnotherCustomer()
    {
        var sut = new PaymentWorkflow(new FakePaymentRepository(), new FakePaymentEventPublisher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ProcessAsync(new CreatePaymentRequest
        {
            CustomerId = Guid.NewGuid(),
            Amount = 100
        }, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_AllowsAdminToPayForAnotherCustomer()
    {
        var repository = new FakePaymentRepository();
        var sut = new PaymentWorkflow(repository, new FakePaymentEventPublisher());

        var payment = await sut.ProcessAsync(new CreatePaymentRequest
        {
            CustomerId = Guid.NewGuid(),
            PolicyId = Guid.NewGuid(),
            Amount = 100
        }, new CurrentUser(Guid.NewGuid(), "Admin"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Single(repository.Payments);
    }

    [Fact]
    public async Task ProcessAsync_CreatesManualCompletedPayment()
    {
        var repository = new FakePaymentRepository();
        var publisher = new FakePaymentEventPublisher();
        var sut = new PaymentWorkflow(repository, publisher);
        var customerId = Guid.NewGuid();

        var payment = await sut.ProcessAsync(new CreatePaymentRequest
        {
            CustomerId = customerId,
            PolicyId = Guid.NewGuid(),
            Amount = 2500
        }, new CurrentUser(customerId, "Customer"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("Manual", payment.Source);
        Assert.Single(repository.Payments);
        Assert.Equal("PaymentCompleted", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task ProcessPolicyPaymentAsync_SetsWorkflowFields()
    {
        var repository = new FakePaymentRepository();
        var publisher = new FakePaymentEventPublisher();
        var sut = new PaymentWorkflow(repository, publisher);

        var payment = await sut.ProcessPolicyPaymentAsync(new PolicyPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500, "PAY-REF", "Purchase"), CancellationToken.None);

        Assert.Equal("PolicyWorkflow", payment.Source);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("PAY-REF", payment.PaymentReference);
        Assert.Equal("PaymentCompleted", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task ProcessPolicyPaymentAsync_PublishesPaymentFailedWhenReferenceRequestsFailure()
    {
        var repository = new FakePaymentRepository();
        var publisher = new FakePaymentEventPublisher();
        var sut = new PaymentWorkflow(repository, publisher);

        var payment = await sut.ProcessPolicyPaymentAsync(new PolicyPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1500, "FAIL-REF", "Renewal"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("PaymentFailed", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task GetAsync_ReturnsRepositoryResults()
    {
        var repository = new FakePaymentRepository();
        repository.Payments.Add(new Payment { CustomerId = Guid.NewGuid(), Amount = 100, Status = PaymentStatus.Completed, Source = "Manual" });
        repository.Payments.Add(new Payment { CustomerId = Guid.NewGuid(), Amount = 200, Status = PaymentStatus.Completed, Source = "Manual" });
        var sut = new PaymentWorkflow(repository, new FakePaymentEventPublisher());

        var payments = await sut.GetAsync(CancellationToken.None);

        Assert.Equal(2, payments.Count);
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Payments { get; } = [];
        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            Payments.Add(payment);
            return Task.CompletedTask;
        }
        public Task<List<Payment>> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Payments.ToList());
        public Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
            => Task.FromResult(Payments.Where(payment => payment.CustomerId == customerId).ToList());
        public Task<bool> ExistsByReferenceAsync(string paymentReference, CancellationToken cancellationToken)
            => Task.FromResult(Payments.Any(payment => string.Equals(payment.PaymentReference, paymentReference, StringComparison.OrdinalIgnoreCase)));
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePaymentEventPublisher : IEventPublisher
    {
        public List<(string EventType, object Payload)> Published { get; } = [];
        public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            Published.Add((eventType, payload));
            return Task.CompletedTask;
        }
    }
}
