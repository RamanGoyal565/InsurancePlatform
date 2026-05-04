using System.ComponentModel.DataAnnotations;

namespace PolicyService.Models;

public enum CustomerPolicyStatus { Pending = 1, Active = 2, Renewed = 3, Cancelled = 4, Expired = 5 }
public enum VehicleType { Car = 1, Truck = 2, Bike = 3 }
public enum PolicyPaymentOperation { Purchase = 1, Renewal = 2 }

public sealed class Policy
{
    [Key] 
    public Guid PolicyId { get; set; } = Guid.NewGuid();
    [MaxLength(200)] 
    public required string Name { get; set; }
    public VehicleType VehicleType { get; set; }
    public decimal Premium { get; set; }
    [MaxLength(2000)] 
    public required string CoverageDetails { get; set; }
    [MaxLength(2000)] 
    public required string Terms { get; set; }
    // No MaxLength — stores base64-encoded PDF which can be several hundred KB
    public required string PolicyDocument { get; set; }
}
