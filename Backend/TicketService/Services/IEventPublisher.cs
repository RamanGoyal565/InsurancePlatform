namespace TicketService.Services
{
    public interface IEventPublisher
    {
        Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken);
    }
}
