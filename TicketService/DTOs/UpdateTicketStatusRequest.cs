using System.ComponentModel.DataAnnotations;
using TicketService.Models;

namespace TicketService.DTOs
{
    public sealed class UpdateTicketStatusRequest
    {
        [Required]
        public TicketStatus Status { get; set; }
    }
}
