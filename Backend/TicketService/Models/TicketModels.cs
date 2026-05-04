using System.ComponentModel.DataAnnotations;

namespace TicketService.Models
{
    public enum TicketType { Support = 1, Claim = 2 }
    public enum TicketStatus { Open = 1, InReview = 2, Assigned = 3, Resolved = 4, Rejected = 5, Closed = 6 }
    public enum ClaimApprovalStatus { Pending = 1, Verified = 2, Approved = 3, Rejected = 4 }
    public sealed class Ticket
    {
        [Key] 
        public Guid TicketId { get; set; } = Guid.NewGuid();
        [MaxLength(200)] 
        public required string Title { get; set; }
        [MaxLength(4000)] 
        public required string Description { get; set; }
        public TicketType Type { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public Guid CustomerId { get; set; }
        public Guid? AssignedTo { get; set; }
        public Guid? PolicyId { get; set; }
        /// <summary>Base64-encoded PDF attached when the ticket was created (optional).</summary>
        public string? DocumentBase64 { get; set; }
        [MaxLength(260)]
        public string? DocumentFileName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<Comment> Comments { get; set; } = [];
        public ClaimDetails? ClaimDetails { get; set; }
    }
}
