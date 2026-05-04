using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs
{
    public sealed class RegisterRequest
    {
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    }
    public sealed record UserResponse(Guid UserId, string Name, string Email, string Role, bool IsActive, DateTime CreatedAt);
    public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
}