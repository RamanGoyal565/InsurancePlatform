namespace PaymentService.Config;

public sealed class RazorpayOptions
{
    public const string SectionName = "Razorpay";
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
}
