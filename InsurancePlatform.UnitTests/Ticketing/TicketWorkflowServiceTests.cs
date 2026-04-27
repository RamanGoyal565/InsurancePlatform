using TicketService.DTOs;
using TicketService.Models;
using TicketService.Repositories;
using TicketService.Services;
using Xunit;

using TicketWorkflow = TicketService.Services.TicketWorkflowService;

namespace InsurancePlatform.UnitTests.Ticketing;

public sealed class TicketWorkflowServiceTests
{
    [Fact]
    public async Task CreateAsync_ThrowsForNonCustomer()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Support",
            Description = "Need help",
            Type = TicketType.Support
        }, new CurrentUser(Guid.NewGuid(), "Admin"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ThrowsForClaimWithoutAmount()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Claim",
            Description = "Need claim",
            Type = TicketType.Claim,
            PolicyId = Guid.NewGuid()
        }, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_CreatesSupportTicketWithoutClaimDetails()
    {
        var repository = new FakeTicketRepository();
        var validator = new FakePolicyValidationService();
        var publisher = new FakeTicketEventPublisher();
        var sut = new TicketWorkflow(repository, publisher, validator);

        var ticket = await sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Support needed",
            Description = "Need help",
            Type = TicketType.Support
        }, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None);

        Assert.Null(ticket.ClaimDetails);
        Assert.Single(repository.Tickets);
        Assert.Equal("TicketCreated", publisher.Published.Single().EventType);
        Assert.True(validator.WasCalled);
    }

    [Fact]
    public async Task CreateAsync_PropagatesPolicyValidationFailure()
    {
        var sut = new TicketWorkflow(new FakeTicketRepository(), new FakeTicketEventPublisher(), new FakePolicyValidationService
        {
            ExceptionToThrow = new InvalidOperationException("Policy does not exist.")
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Claim",
            Description = "Need claim",
            Type = TicketType.Claim,
            ClaimAmount = 100,
            PolicyId = Guid.NewGuid()
        }, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None));

        Assert.Equal("Policy does not exist.", error.Message);
    }

    [Fact]
    public async Task GetAsync_FiltersTicketsByRoleAndAssignment()
    {
        var customerId = Guid.NewGuid();
        var claimSpecialistId = Guid.NewGuid();
        var supportSpecialistId = Guid.NewGuid();
        var repository = new FakeTicketRepository();

        var customerTicket = CreateTicket(TicketType.Support, TicketStatus.Open, customerId);
        var assignedClaim = CreateTicket(TicketType.Claim, TicketStatus.Assigned, Guid.NewGuid());
        assignedClaim.AssignedTo = claimSpecialistId;
        var unassignedClaim = CreateTicket(TicketType.Claim, TicketStatus.Open, Guid.NewGuid());
        var assignedSupport = CreateTicket(TicketType.Support, TicketStatus.Assigned, Guid.NewGuid());
        assignedSupport.AssignedTo = supportSpecialistId;
        var someoneElsesSupport = CreateTicket(TicketType.Support, TicketStatus.Assigned, Guid.NewGuid());
        someoneElsesSupport.AssignedTo = Guid.NewGuid();

        repository.Tickets.AddRange([customerTicket, assignedClaim, unassignedClaim, assignedSupport, someoneElsesSupport]);
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        var customerTickets = await sut.GetAsync(new CurrentUser(customerId, "Customer"), CancellationToken.None);
        var claimTickets = await sut.GetAsync(new CurrentUser(claimSpecialistId, "ClaimsSpecialist"), CancellationToken.None);
        var supportTickets = await sut.GetAsync(new CurrentUser(supportSpecialistId, "SupportSpecialist"), CancellationToken.None);

        Assert.Single(customerTickets);
        Assert.Single(claimTickets);
        Assert.Equal(assignedClaim.TicketId, claimTickets.Single().TicketId);
        Assert.Single(supportTickets);
        Assert.Equal(assignedSupport.TicketId, supportTickets.Single().TicketId);
    }

    [Fact]
    public async Task GetCommentsAsync_RejectsUnrelatedCustomer()
    {
        var repository = new FakeTicketRepository();
        var ownerId = Guid.NewGuid();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open, ownerId);
        repository.Tickets.Add(ticket);
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetCommentsAsync(ticket.TicketId, new CurrentUser(Guid.NewGuid(), "Customer"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_RequiresAssignmentForSpecialist()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open);
        repository.Tickets.Add(ticket);
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateStatusAsync(ticket.TicketId, new UpdateTicketStatusRequest { Status = TicketStatus.InReview }, new CurrentUser(Guid.NewGuid(), "SupportSpecialist"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_AllowsAssignedSpecialist()
    {
        var specialistId = Guid.NewGuid();
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Assigned);
        ticket.AssignedTo = specialistId;
        repository.Tickets.Add(ticket);
        var publisher = new FakeTicketEventPublisher();
        var sut = new TicketWorkflow(repository, publisher, new FakePolicyValidationService());

        var updated = await sut.UpdateStatusAsync(ticket.TicketId, new UpdateTicketStatusRequest { Status = TicketStatus.InReview }, new CurrentUser(specialistId, "SupportSpecialist"), CancellationToken.None);

        Assert.Equal(TicketStatus.InReview, updated.Status);
        Assert.Equal("TicketUpdated", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task UpdateStatusAsync_AdminAutoAssignsTicket()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open);
        repository.Tickets.Add(ticket);
        var adminId = Guid.NewGuid();
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        var updated = await sut.UpdateStatusAsync(ticket.TicketId, new UpdateTicketStatusRequest { Status = TicketStatus.Resolved }, new CurrentUser(adminId, "Admin"), CancellationToken.None);

        Assert.Equal(adminId, updated.AssignedTo);
        Assert.Equal(TicketStatus.Resolved, updated.Status);
    }

    [Fact]
    public async Task AssignAsync_RejectsNonAdmin()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open);
        repository.Tickets.Add(ticket);
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.AssignAsync(ticket.TicketId, new AssignTicketRequest { AssignedToUserId = Guid.NewGuid() }, new CurrentUser(Guid.NewGuid(), "SupportSpecialist"), CancellationToken.None));
    }

    [Fact]
    public async Task AssignAsync_UpdatesAssignmentAndPublishesEvent()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open);
        repository.Tickets.Add(ticket);
        var publisher = new FakeTicketEventPublisher();
        var assignedUserId = Guid.NewGuid();
        var sut = new TicketWorkflow(repository, publisher, new FakePolicyValidationService());

        var updated = await sut.AssignAsync(ticket.TicketId, new AssignTicketRequest { AssignedToUserId = assignedUserId }, new CurrentUser(Guid.NewGuid(), "Admin"), CancellationToken.None);

        Assert.Equal(assignedUserId, updated.AssignedTo);
        Assert.Equal(TicketStatus.Assigned, updated.Status);
        Assert.Equal("TicketAssigned", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task AddCommentAsync_AllowsCustomerOwner()
    {
        var repository = new FakeTicketRepository();
        var customerId = Guid.NewGuid();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Open, customerId);
        repository.Tickets.Add(ticket);
        var publisher = new FakeTicketEventPublisher();
        var sut = new TicketWorkflow(repository, publisher, new FakePolicyValidationService());

        var updated = await sut.AddCommentAsync(ticket.TicketId, new AddCommentRequest { Message = "Any update?" }, new CurrentUser(customerId, "Customer"), CancellationToken.None);

        Assert.Single(updated.Comments);
        Assert.Equal("CommentAdded", publisher.Published.Single().EventType);
    }

    [Fact]
    public async Task ApproveClaimAsync_RejectsNonClaimTicket()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Support, TicketStatus.Assigned);
        ticket.AssignedTo = Guid.NewGuid();
        repository.Tickets.Add(ticket);
        var sut = new TicketWorkflow(repository, new FakeTicketEventPublisher(), new FakePolicyValidationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveClaimAsync(ticket.TicketId, new CurrentUser(ticket.AssignedTo!.Value, "ClaimsSpecialist"), CancellationToken.None));
    }

    [Fact]
    public async Task RejectClaimAsync_RequiresAssignedClaimsSpecialist()
    {
        var repository = new FakeTicketRepository();
        var ticket = CreateTicket(TicketType.Claim, TicketStatus.Assigned);
        ticket.AssignedTo = Guid.NewGuid();
        ticket.ClaimDetails = new ClaimDetails { TicketId = ticket.TicketId, ClaimAmount = 100, Documents = "doc" };
        repository.Tickets.Add(ticket);
        var publisher = new FakeTicketEventPublisher();
        var sut = new TicketWorkflow(repository, publisher, new FakePolicyValidationService());

        var updated = await sut.RejectClaimAsync(ticket.TicketId, new CurrentUser(ticket.AssignedTo.Value, "ClaimsSpecialist"), CancellationToken.None);

        Assert.Equal(TicketStatus.Rejected, updated.Status);
        Assert.Equal(ClaimApprovalStatus.Rejected, updated.ClaimDetails!.ApprovalStatus);
        Assert.Equal("ClaimRejected", publisher.Published.Single().EventType);
    }

    private static TicketWorkflow CreateSut() => new(new FakeTicketRepository(), new FakeTicketEventPublisher(), new FakePolicyValidationService());

    private static Ticket CreateTicket(TicketType type, TicketStatus status, Guid? customerId = null) => new()
    {
        TicketId = Guid.NewGuid(),
        Title = "Ticket",
        Description = "Description",
        Type = type,
        Status = status,
        CustomerId = customerId ?? Guid.NewGuid(),
        UpdatedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        Comments = []
    };

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public List<Ticket> Tickets { get; } = [];
        public List<Comment> Comments { get; } = [];

        public Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            Tickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task AddCommentAsync(Comment comment, CancellationToken cancellationToken)
        {
            Comments.Add(comment);
            var ticket = Tickets.Single(x => x.TicketId == comment.TicketId);
            ticket.Comments.Add(comment);
            return Task.CompletedTask;
        }

        public Task<Ticket?> GetAsync(Guid ticketId, CancellationToken cancellationToken) => Task.FromResult(Tickets.SingleOrDefault(x => x.TicketId == ticketId));
        public Task<List<Ticket>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(Tickets.ToList());
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTicketEventPublisher : IEventPublisher
    {
        public List<(string EventType, object Payload)> Published { get; } = [];
        public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken)
        {
            Published.Add((eventType, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class FakePolicyValidationService : IPolicyValidationService
    {
        public bool WasCalled { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public Task ValidateForTicketCreationAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            WasCalled = true;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
