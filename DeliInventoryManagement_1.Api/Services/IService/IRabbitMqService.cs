using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Services.IService;

public interface IRabbitMqService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected { get; }
    Task CreateQueuesAsync();

    Task PublishSaleCreatedAsync(SaleV5 sale);
    Task PublishSaleUpdatedAsync(SaleV5 sale);
    Task PublishSaleCancelledAsync(string saleId, string reason);

    Task PublishRestockCreatedAsync(RestockV5 restock);

    Task PublishLowStockAlertAsync(ProductV5 product, int currentStock, int minimumStock);
    Task PublishProductUpdatedAsync(ProductV5 product);
    Task PublishProductDiscontinuedAsync(string productId);

    Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue);
    Task PublishToRetryQueueAsync(object message, int retryCount, string queue);

    Task StartConsumerAsync(string queueName, Func<object, Task> handler);
    Task StopAllConsumersAsync();

    Task<RabbitMqHealthStatus> GetHealthStatusAsync();
}

public sealed class RabbitMqHealthStatus
{
    public bool IsHealthy { get; set; }
    public bool IsConnected { get; set; }
    public int QueueCount { get; set; }
    public int MessageCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
}