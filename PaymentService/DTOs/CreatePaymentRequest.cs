using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs
{
    public sealed class CreatePaymentRequest
    {
        [Required] 
        public Guid CustomerId { get; set; }
        public Guid? PolicyId { get; set; }
        [Range(0.01, 10000000)] 
        public decimal Amount { get; set; }
    }
}