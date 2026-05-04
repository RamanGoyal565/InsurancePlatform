using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models;

public sealed class Notification
{
    [Key] public Guid NotificationId { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    [MaxLength(2000)] public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class UserContact
{
    [Key] public Guid UserId { get; set; }
    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(320)] public required string Email { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
