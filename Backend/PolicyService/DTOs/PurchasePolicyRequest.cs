using System.ComponentModel.DataAnnotations;

namespace PolicyService.DTOs
{
    public sealed class PurchasePolicyRequest
    {
        [Required] 
        public Guid PolicyId { get; set; }
        [Required, MaxLength(50)] 
        public string VehicleNumber { get; set; } = string.Empty;
        [Required, MaxLength(50)] 
        public string DrivingLicenseNumber { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
    }
}
