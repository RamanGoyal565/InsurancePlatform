using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;

namespace IdentityService.Services;

public sealed class OtpService(
    IdentityDbContext dbContext,
    IUserRepository userRepository,
    IPasswordHasherService passwordHasher,
    IEventPublisher eventPublisher) : IOtpService
{
    private const int OtpExpiryMinutes = 10;

    public async Task<OtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        // For security: always return success even if email not found (prevents enumeration)
        if (user is null)
            return new OtpResponse(true, "If an account exists for that email, an OTP has been sent.");

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, out var purpose))
            throw new InvalidOperationException("Invalid OTP purpose.");

        // Invalidate any existing unused OTPs for this user+purpose
        var existing = await dbContext.OtpTokens
            .Where(x => x.UserId == user.UserId && x.Purpose == purpose && !x.IsUsed)
            .ToListAsync(cancellationToken);
        foreach (var old in existing) old.IsUsed = true;

        // Generate 6-digit OTP
        var code = Random.Shared.Next(100000, 999999).ToString();

        dbContext.OtpTokens.Add(new OtpToken
        {
            UserId = user.UserId,
            Code = code,
            Purpose = purpose,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            IsUsed = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        // Publish event so NotificationService sends the OTP email
        var eventType = purpose == OtpPurpose.ForgotPassword ? "OtpForgotPassword" : "OtpEmailVerification";
        await eventPublisher.PublishAsync(eventType, new
        {
            user.UserId,
            user.Name,
            user.Email,
            Code = code,
            ExpiryMinutes = OtpExpiryMinutes
        }, cancellationToken);

        return new OtpResponse(true, "If an account exists for that email, an OTP has been sent.");
    }

    public async Task<OtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null) return new OtpResponse(false, "Invalid OTP.");

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, out var purpose))
            throw new InvalidOperationException("Invalid OTP purpose.");

        var token = await dbContext.OtpTokens
            .Where(x => x.UserId == user.UserId
                     && x.Purpose == purpose
                     && x.Code == request.Code
                     && !x.IsUsed
                     && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null) return new OtpResponse(false, "Invalid or expired OTP.");

        // For email verification, mark verified immediately
        if (purpose == OtpPurpose.EmailVerification)
        {
            token.IsUsed = true;
            user.IsEmailVerified = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new OtpResponse(true, "OTP verified successfully.");
    }

    public async Task<OtpResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null) return new OtpResponse(false, "Invalid request.");

        var token = await dbContext.OtpTokens
            .Where(x => x.UserId == user.UserId
                     && x.Purpose == OtpPurpose.ForgotPassword
                     && x.Code == request.Code
                     && !x.IsUsed
                     && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null) return new OtpResponse(false, "Invalid or expired OTP.");

        token.IsUsed = true;
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OtpResponse(true, "Password reset successfully. You can now log in with your new password.");
    }
}
