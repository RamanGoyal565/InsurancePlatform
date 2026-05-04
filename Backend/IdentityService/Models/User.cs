using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models
{
    public enum UserRole
    {
        Customer = 1,
        ClaimsSpecialist = 2,
        SupportSpecialist = 3,
        Admin = 4
    }
    public sealed class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();
        [MaxLength(200)]
        public required string Name { get; set; }
        [MaxLength(320)]
        public required string Email { get; set; }
        [MaxLength(500)]
        public required string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}