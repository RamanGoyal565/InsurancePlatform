using System.ComponentModel.DataAnnotations;

namespace PolicyService.Models
{
    public sealed class CustomerPolicy
    {
        [Key]
        public Guid CustomerPolicyId { get; set; } = Guid.NewGuid();
        public Guid PolicyId { get; set; }
        public Policy? Policy { get; set; }
        public Guid CustomerId { get; set; }
        [MaxLength(50)]
        public required string VehicleNumber { get; set; }
        [MaxLength(50)]
        public required string DrivingLicenseNumber { get; set; }
        public PolicyPaymentOperation? PendingOperation { get; set; }
        [MaxLength(500)]
        public string? LastPaymentFailureReason { get; set; }
        public DateTime? LastPaymentFailedOnUtc { get; set; }
        public DateTime? LastMonthlyWindowReminderSentOnUtc { get; set; }
        public DateTime? LastFinalWeekReminderSentOnUtc { get; set; }
        public DateTime? ExpiredNotifiedOnUtc { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public CustomerPolicyStatus Status { get; set; } = CustomerPolicyStatus.Active;
    }
}
