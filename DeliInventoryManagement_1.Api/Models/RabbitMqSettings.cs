namespace DeliInventoryManagement_1.Core.Models
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string? ExchangeName { get; set; }
        public string ExchangeType { get; set; } = "direct";
        public ushort PrefetchCount { get; set; } = 10;
        public bool Enabled { get; set; } = true;
    }
}