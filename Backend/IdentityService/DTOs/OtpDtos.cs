using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public sealed class RequestOtpRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Purpose { get; set; } = string.Empty; // "ForgotPassword" | "EmailVerification"
}

public sealed class VerifyOtpRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(6)] public string Code { get; set; } = string.Empty;
    [Required] public string Purpose { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(6)] public string Code { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public sealed record OtpResponse(bool Success, string Message);
