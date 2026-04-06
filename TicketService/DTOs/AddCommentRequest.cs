using System.ComponentModel.DataAnnotations;

namespace TicketService.DTOs
{
    public sealed class AddCommentRequest
    {
        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
