namespace TicketService.Config
{
    public sealed class PolicyValidationOptions
    {
        public const string SectionName = "PolicyValidation";
        public string BaseUrl { get; set; } = "http://localhost:5000";
    }
}
