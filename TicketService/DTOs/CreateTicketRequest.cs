using System.ComponentModel.DataAnnotations;
using TicketService.Models;

namespace TicketService.DTOs
{
    public sealed class CreateTicketRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [Required, MaxLength(4000)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public TicketType Type { get; set; }
        public Guid? PolicyId { get; set; }
        public decimal? ClaimAmount { get; set; }
        public string? Documents { get; set; }
    }
}