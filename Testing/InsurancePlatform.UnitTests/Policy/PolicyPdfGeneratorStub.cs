using PolicyService.Models;

namespace PolicyService.Services;

internal static class PolicyPdfGenerator
{
    public static string GenerateBase64(Policy policy)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"""
            Policy Document
            Policy Id: {policy.PolicyId}
            Name: {policy.Name}
            Vehicle Type: {policy.VehicleType}
            Annual Premium: {policy.Premium}
            Coverage Details: {policy.CoverageDetails}
            Terms: {policy.Terms}
            """));
}
