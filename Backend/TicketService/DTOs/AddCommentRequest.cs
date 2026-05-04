using System.ComponentModel.DataAnnotations;

namespace TicketService.DTOs
{
    public sealed class AddCommentRequest
    {
        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        /// <summary>Base64-encoded PDF, max ~1 MB.</summary>
        [MaxLength(1_500_000)]
        public string? DocumentBase64 { get; set; }
        /// <summary>Original filename of the uploaded PDF.</summary>
        [MaxLength(260)]
        public string? DocumentFileName { get; set; }
    }
}
