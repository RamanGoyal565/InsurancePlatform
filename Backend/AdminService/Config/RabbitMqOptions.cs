namespace AdminService.Config
{
    public sealed class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";
        public string HostName { get; set; } = "localhost";
        public string ExchangeName { get; set; } = "insurance.events";
        public string QueueName { get; set; } = "admin-reporting-service";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }
}
