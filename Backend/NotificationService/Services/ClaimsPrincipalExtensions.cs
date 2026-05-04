using System.Security.Claims;

namespace NotificationService.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? Guid.Empty.ToString();
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return new CurrentUser(Guid.Parse(userId), role);
        }
    }
}
