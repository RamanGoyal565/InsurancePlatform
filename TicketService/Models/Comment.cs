using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TicketService.Models
{
    public sealed class Comment
    {
        [Key] 
        public Guid CommentId { get; set; } = Guid.NewGuid();
        public Guid TicketId { get; set; }
        [JsonIgnore]
        public Ticket? Ticket { get; set; }
        public Guid UserId { get; set; }
        [MaxLength(2000)] 
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
