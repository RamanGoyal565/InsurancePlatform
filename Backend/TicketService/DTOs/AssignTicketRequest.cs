using System.ComponentModel.DataAnnotations;

namespace TicketService.DTOs
{
    public sealed class AssignTicketRequest
    {
        [Required]
        public Guid AssignedToUserId { get; set; }
    }
}
