using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs;

/// <summary>Frontend calls this to create a Razorpay order before showing the payment modal.</summary>
public sealed class CreateRazorpayOrderRequest
{
    [Required] public Guid CustomerId { get; set; }
    public Guid? PolicyId { get; set; }
    [Range(1, 10000000)] public decimal Amount { get; set; }
    /// <summary>Optional description shown in the Razorpay modal.</summary>
    [MaxLength(200)] public string? Description { get; set; }
}

public sealed record RazorpayOrderResponse(
    string OrderId,
    decimal Amount,
    string Currency,
    string KeyId);

/// <summary>Frontend sends this after Razorpay payment succeeds to verify and record it.</summary>
public sealed class VerifyRazorpayPaymentRequest
{
    [Required] public Guid CustomerId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? CustomerPolicyId { get; set; }
    [Required] public string RazorpayOrderId { get; set; } = string.Empty;
    [Required] public string RazorpayPaymentId { get; set; } = string.Empty;
    [Required] public string RazorpaySignature { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
