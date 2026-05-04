using IdentityService.Config;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Services
{
    public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public AuthResponse CreateToken(User user)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256));

            return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc, new UserResponse(user.UserId, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt));
        }
    }
}
