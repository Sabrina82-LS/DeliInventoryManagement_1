namespace DeliInventoryManagement_1.Api.Configuration;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";

    public string Exchange { get; set; } = "inventory.events";

    // ✅ Retry/DLX
    public string RetryExchange { get; set; } = "inventory.retry";
    public int RetryTtlMs { get; set; } = 10_000;
}
