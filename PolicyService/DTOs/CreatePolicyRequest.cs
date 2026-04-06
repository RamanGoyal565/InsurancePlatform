using PolicyService.Models;
using System.ComponentModel.DataAnnotations;

namespace PolicyService.DTOs
{

    public sealed class CreatePolicyRequest
    {
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        [Required] public VehicleType VehicleType { get; set; }
        [Range(0.01, 10000000)] public decimal Premium { get; set; }
        [Required, MaxLength(2000)] public string CoverageDetails { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Terms { get; set; } = string.Empty;
        [MaxLength(8000)] public string? PolicyDocument { get; set; }
    }
}
