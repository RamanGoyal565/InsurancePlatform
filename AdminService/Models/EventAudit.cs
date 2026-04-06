using System.ComponentModel.DataAnnotations;

namespace AdminService.Models;

public sealed class EventAudit
{
    [Key] public Guid EventAuditId { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public required string EventType { get; set; }
    [MaxLength(4000)] public required string Payload { get; set; }
    public DateTime OccurredOnUtc { get; set; }
}
