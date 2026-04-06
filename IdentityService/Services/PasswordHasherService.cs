using IdentityService.DTOs;
using IdentityService.Models;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Services
{
    public sealed class PasswordHasherService : IPasswordHasherService
    {
        public string Hash(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}")));
            return $"{salt}.{hash}";
        }

        public bool Verify(string password, string storedHash)
        {
            var parts = storedHash.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            var attemptedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{parts[0]}:{password}")));
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(attemptedHash), Encoding.UTF8.GetBytes(parts[1]));
        }
    }
}
