using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.Config;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Services;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Controllers;

[ApiController]
[Authorize]
[Route("payments/razorpay")]
public sealed class RazorpayController(
    IPaymentRepository repository,
    IEventPublisher eventPublisher,
    IOptions<RazorpayOptions> razorpayOptions) : ControllerBase
{
    private readonly RazorpayOptions _options = razorpayOptions.Value;

    /// <summary>
    /// Step 1: Create a Razorpay order. Frontend uses the returned orderId to open the payment modal.
    /// </summary>
    [HttpPost("create-order")]
    public ActionResult<RazorpayOrderResponse> CreateOrder(CreateRazorpayOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId) || _options.KeyId.StartsWith("rzp_test_REPLACE"))
            return StatusCode(503, new { message = "Razorpay is not configured. Add KeyId and KeySecret to appsettings.json." });

        var client = new RazorpayClient(_options.KeyId, _options.KeySecret);

        // Razorpay amounts are in paise (1 INR = 100 paise)
        var amountInPaise = (int)(request.Amount * 100);

        var orderOptions = new Dictionary<string, object>
        {
            ["amount"] = amountInPaise,
            ["currency"] = _options.Currency,
            ["receipt"] = $"rcpt_{Guid.NewGuid():N}",
            ["notes"] = new Dictionary<string, string>
            {
                ["customerId"] = request.CustomerId.ToString(),
                ["policyId"] = request.PolicyId?.ToString() ?? string.Empty,
                ["description"] = request.Description ?? "Insurance Premium Payment"
            }
        };

        var order = client.Order.Create(orderOptions);
        var orderId = order["id"].ToString();

        return Ok(new RazorpayOrderResponse(orderId!, request.Amount, _options.Currency, _options.KeyId));
    }

    /// <summary>
    /// Step 2: Verify Razorpay signature and record the payment.
    /// Called after the user completes payment in the Razorpay modal.
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult> VerifyPayment(
        VerifyRazorpayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.KeySecret))
            return StatusCode(503, new { message = "Razorpay is not configured." });

        // Verify HMAC-SHA256 signature: orderId|paymentId signed with KeySecret
        var expectedSignature = ComputeHmacSha256(
            $"{request.RazorpayOrderId}|{request.RazorpayPaymentId}",
            _options.KeySecret);

        if (!string.Equals(expectedSignature, request.RazorpaySignature, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Payment signature verification failed." });

        // Record the payment
        var payment = new PaymentService.Models.Payment
        {
            CustomerId = request.CustomerId,
            PolicyId = request.PolicyId,
            CustomerPolicyId = request.CustomerPolicyId,
            PaymentReference = request.RazorpayPaymentId,
            Source = "Razorpay",
            Amount = request.Amount,
            Status = PaymentStatus.Completed
        };

        await repository.AddAsync(payment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("PaymentCompleted", new
        {
            payment.PaymentId,
            payment.CustomerId,
            payment.PolicyId,
            payment.CustomerPolicyId,
            payment.Amount,
            payment.PaymentReference,
            payment.Source,
            Operation = "Manual"
        }, cancellationToken);

        return Ok(new { message = "Payment verified and recorded successfully.", paymentId = payment.PaymentId });
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
