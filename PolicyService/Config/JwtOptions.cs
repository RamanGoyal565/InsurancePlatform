namespace PolicyService.Config
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";
        public string Issuer { get; set; } = "InsurancePlatform";
        public string Audience { get; set; } = "InsurancePlatformClients";
        public string Key { get; set; } = "insurance-platform-jwt-key-change-me-before-production-12345";
    }
}
