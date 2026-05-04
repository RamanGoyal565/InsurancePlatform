using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services
{   
    public sealed class IdentityService(IUserRepository userRepository, IPasswordHasherService passwordHasherService, IJwtTokenService jwtTokenService, IEventPublisher eventPublisher) : IIdentityService
    {
        public async Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
                throw new InvalidOperationException("A user with this email already exists.");
            var user = new User
            { 
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHasherService.Hash(request.Password),
                Role = UserRole.Customer,
                IsActive = true 
            };
            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("UserRegistered", new 
            { 
                user.UserId,
                user.Name,
                user.Email, 
                Role = user.Role.ToString() 
            },
            cancellationToken);
            return jwtTokenService.CreateToken(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken) ?? throw new UnauthorizedAccessException("Invalid email or password.");
            if (!user.IsActive) 
                throw new UnauthorizedAccessException("This user is inactive.");
            if (!passwordHasherService.Verify(request.Password, user.PasswordHash)) 
                throw new UnauthorizedAccessException("Invalid email or password.");
            return jwtTokenService.CreateToken(user);
        }

        public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, UserRole role, CancellationToken cancellationToken)
        {
            if (role is UserRole.Customer or UserRole.Admin) 
                throw new InvalidOperationException("This endpoint does not support the selected role.");
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken)) 
                throw new InvalidOperationException("A user with this email already exists.");

            var user = new User
            { 
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHasherService.Hash(request.Password),
                Role = role,
                IsActive = true
            };
            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("UserRegistered", new 
            { 
                user.UserId, 
                user.Name, 
                user.Email, 
                Role = user.Role.ToString() 
            }, cancellationToken);
            return new UserResponse(user.UserId, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
        }

        public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken)
        {
            var users = await userRepository.GetAllAsync(cancellationToken);
            return users.Select(x => new UserResponse(x.UserId, x.Name, x.Email, x.Role.ToString(), x.IsActive, x.CreatedAt)).ToList();
        }

        public async Task<UserResponse> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
            user.IsActive = isActive;
            await userRepository.SaveChangesAsync(cancellationToken);
            await eventPublisher.PublishAsync("UserStatusUpdated", new 
            { 
                user.UserId, 
                user.Name, 
                user.Email, 
                Role = user.Role.ToString(), 
                user.IsActive 
            }, cancellationToken);
            return new UserResponse(user.UserId, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
        }

        public async Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) return null;
            return new UserResponse(user.UserId, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
        }
    }
}
