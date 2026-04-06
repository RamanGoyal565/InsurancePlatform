using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IIdentityService
    {
        Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
        Task<UserResponse> CreateUserAsync(CreateUserRequest request, UserRole role, CancellationToken cancellationToken);
        Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken);
        Task<UserResponse> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    }
}
