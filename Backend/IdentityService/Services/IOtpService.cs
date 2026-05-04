using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IOtpService
{
    Task<OtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken);
    Task<OtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);
    Task<OtpResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
