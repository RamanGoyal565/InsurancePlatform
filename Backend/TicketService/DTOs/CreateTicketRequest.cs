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
        /// <summary>Base64-encoded PDF, max ~1 MB (base64 ≈ 1.37× raw, so limit at 1.4 MB base64 string).</summary>
        [MaxLength(1_500_000)]
        public string? DocumentBase64 { get; set; }
        /// <summary>Original filename of the uploaded PDF.</summary>
        [MaxLength(260)]
        public string? DocumentFileName { get; set; }
    }
}