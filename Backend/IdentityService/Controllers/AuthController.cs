using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(IIdentityService identityService, IOtpService otpService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return Forbid();
        return Ok(await identityService.RegisterCustomerAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.LoginAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        var user = await identityService.GetByIdAsync(userId, cancellationToken);
        if (user is null) return Unauthorized();
        return Ok(user);
    }

    /// <summary>Request a 6-digit OTP for forgot-password or email verification.</summary>
    [AllowAnonymous]
    [HttpPost("otp/request")]
    public async Task<ActionResult<OtpResponse>> RequestOtp(RequestOtpRequest request, CancellationToken cancellationToken)
        => Ok(await otpService.RequestOtpAsync(request, cancellationToken));

    /// <summary>Verify an OTP (used for email verification step).</summary>
    [AllowAnonymous]
    [HttpPost("otp/verify")]
    public async Task<ActionResult<OtpResponse>> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
        => Ok(await otpService.VerifyOtpAsync(request, cancellationToken));

    /// <summary>Reset password using a verified OTP code.</summary>
    [AllowAnonymous]
    [HttpPost("otp/reset-password")]
    public async Task<ActionResult<OtpResponse>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        => Ok(await otpService.ResetPasswordAsync(request, cancellationToken));
}
