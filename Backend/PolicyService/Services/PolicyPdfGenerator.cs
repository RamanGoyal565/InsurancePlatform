using PolicyService.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PolicyService.Services;

/// <summary>
/// Generates a policy document PDF and returns it as a base64 string.
/// </summary>
public static class PolicyPdfGenerator
{
    static PolicyPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string GenerateBase64(Policy policy, CustomerPolicy? customerPolicy = null)
    {
        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                // ── Header ────────────────────────────────────────────────────
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("TRUST GUARD")
                                .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().Text("Policy Document")
                                .FontSize(12).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(140).AlignRight().AlignMiddle()
                            .Text($"Date: {DateTime.UtcNow:dd MMM yyyy}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });

                    header.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);
                });

                // ── Content ───────────────────────────────────────────────────
                page.Content().PaddingTop(16).Column(col =>
                {
                    // Policy overview banner
                    col.Item()
                        .Background(Colors.Blue.Lighten5)
                        .Padding(12)
                        .Column(inner =>
                        {
                            inner.Item().Text(policy.Name)
                                .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);

                            inner.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem()
                                    .Text($"Vehicle Type: {policy.VehicleType}")
                                    .FontSize(11);

                                row.RelativeItem().AlignRight()
                                    .Text($"Annual Premium: \u20B9{policy.Premium:N2}")
                                    .FontSize(11).Bold();
                            });
                        });

                    // Customer / policy details (only when purchased)
                    if (customerPolicy is not null)
                    {
                        col.Item().PaddingTop(16).Text("Policy Details")
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            void AddRow(string label, string value)
                            {
                                table.Cell().Background(Colors.Grey.Lighten4).Padding(6)
                                    .Text(label).Bold();
                                table.Cell().Padding(6).Text(value);
                            }

                            AddRow("Policy Number",
                                $"POL-{customerPolicy.CustomerPolicyId.ToString()[..8].ToUpper()}");
                            AddRow("Vehicle Number", customerPolicy.VehicleNumber);
                            AddRow("Driving License", customerPolicy.DrivingLicenseNumber);
                            AddRow("Start Date", customerPolicy.StartDate.ToString("dd MMM yyyy"));
                            AddRow("End Date", customerPolicy.EndDate.ToString("dd MMM yyyy"));
                            AddRow("Status", customerPolicy.Status.ToString());
                        });
                    }

                    // Coverage details
                    col.Item().PaddingTop(16).Text("Coverage Details")
                        .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                    // Split by semicolon only — items may contain commas internally
                    // (e.g. "Own damage (collision, accident, overturning)").
                    var coverageItems = policy.CoverageDetails
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var item in coverageItems)
                    {
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.ConstantItem(20)
                                .Text("\u2713").Bold().FontColor(Colors.Green.Darken2);
                            row.RelativeItem().Text(item);
                        });
                    }

                    // Terms & conditions
                    col.Item().PaddingTop(16).Text("Terms & Conditions")
                        .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingTop(6)
                        .Text(policy.Terms)
                        .FontSize(10).FontColor(Colors.Grey.Darken2);

                    // Disclaimer
                    col.Item().PaddingTop(24)
                        .Background(Colors.Yellow.Lighten4)
                        .Padding(10)
                        .Text("This document is for informational purposes only. "
                            + "Please refer to your policy schedule for exact coverage terms.")
                        .FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Trust Guard  \u2022  ")
                            .FontColor(Colors.Grey.Medium).FontSize(9);
                        t.Span("This is a computer-generated document.")
                            .FontColor(Colors.Grey.Medium).FontSize(9);
                    });
            });
        }).GeneratePdf();

        return Convert.ToBase64String(pdfBytes);
    }
}
