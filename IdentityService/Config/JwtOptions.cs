namespace IdentityService.Config
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";
        public string Issuer { get; set; } = "InsurancePlatform";
        public string Audience { get; set; } = "InsurancePlatformClients";
        public string Key { get; set; } = "super-secret-development-key-change-me-12345";
        public int ExpiryMinutes { get; set; } = 120;
    }
}
