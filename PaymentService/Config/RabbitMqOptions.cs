namespace PaymentService.Config
{
    public sealed class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";
        public string HostName { get; set; } = "localhost";
        public string ExchangeName { get; set; } = "insurance.events";
        public string PaymentRequestsQueueName { get; set; } = "payment-service-requests";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }
}
