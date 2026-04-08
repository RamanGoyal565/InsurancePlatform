namespace PolicyService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
}

namespace TicketService.Services
{
    public sealed record CurrentUser(Guid UserId, string Role);
}
