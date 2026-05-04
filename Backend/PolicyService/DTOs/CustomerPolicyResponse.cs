using PolicyService.Models;

namespace PolicyService.DTOs
{
    /// <param name="Document">Base64-encoded PDF of the policy document.</param>
    public sealed record PolicyDocumentResponse(Guid PolicyId, string Name, string Document);
    public sealed record TicketPolicyValidationResponse(bool PolicyExists, bool CustomerOwnsPolicy);
    public sealed record CustomerPolicyResponse(
        Guid CustomerPolicyId,
        Guid PolicyId,
        string PolicyName,
        VehicleType VehicleType,
        decimal Premium,
        string VehicleNumber,
        string DrivingLicenseNumber,
        CustomerPolicyStatus Status,
        DateTime StartDate,
        DateTime EndDate,
        string? LastPaymentFailureReason,
        DateTime? LastPaymentFailedOnUtc);
}
