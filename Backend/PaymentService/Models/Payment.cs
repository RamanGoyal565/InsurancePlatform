using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public enum PaymentStatus { Pending = 1, Completed = 2, Failed = 3 }

public sealed class Payment
{
    [Key] 
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? CustomerPolicyId { get; set; }
    [MaxLength(100)] 
    public string PaymentReference { get; set; } = string.Empty;
    [MaxLength(20)] 
    public string Source { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
