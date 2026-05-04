using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models;

public enum OtpPurpose { ForgotPassword = 1, EmailVerification = 2 }

public sealed class OtpToken
{
    [Key] public Guid OtpTokenId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [MaxLength(6)] public required string Code { get; set; }
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
