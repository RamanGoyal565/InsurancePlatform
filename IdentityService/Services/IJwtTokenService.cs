using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IJwtTokenService
    {
        AuthResponse CreateToken(User user);
    }
}
