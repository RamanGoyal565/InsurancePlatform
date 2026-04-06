using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TicketService.Models
{
    public sealed class ClaimDetails
    {
        [Key] public Guid ClaimId { get; set; } = Guid.NewGuid();
        public Guid TicketId { get; set; }
        [JsonIgnore]
        public Ticket? Ticket { get; set; }
        public decimal ClaimAmount { get; set; }
        [MaxLength(2000)] public string Documents { get; set; } = string.Empty;
        public ClaimApprovalStatus ApprovalStatus { get; set; } = ClaimApprovalStatus.Pending;
    }
}
