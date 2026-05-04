using System.Security.Claims;

namespace TicketService.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("Missing user id claim.");
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? throw new UnauthorizedAccessException("Missing role claim.");
            return new CurrentUser(Guid.Parse(userId), role);
        }
    }
}
