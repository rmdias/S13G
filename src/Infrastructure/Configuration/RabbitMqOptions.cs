namespace S13G.Infrastructure.Configuration
{
    public class RabbitMqOptions
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string ExchangeName { get; set; } = "documents";
        public string QueueName { get; set; } = "documents.processed";
        public string DeadLetterExchange { get; set; } = "documents.dlx";
        public int RetryCount { get; set; } = 5;
        public int RetryInitialDelayMs { get; set; } = 500;
    }
}