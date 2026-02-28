using System;
using System.Threading.Tasks;
using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Services
{
    public interface IRabbitMqService
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
        Task CreateQueuesAsync();

        Task PublishSaleCreatedAsync(Sale sale);
        Task PublishSaleUpdatedAsync(Sale sale);
        Task PublishSaleCancelledAsync(string saleId, string reason);

        Task PublishRestockCreatedAsync(Restock restock);
        Task PublishLowStockAlertAsync(Product product, int currentStock, int minimumStock);

        Task PublishProductUpdatedAsync(Product product);
        Task PublishProductDiscontinuedAsync(string productId);

        Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue);
        Task PublishToRetryQueueAsync(object message, int retryCount, string queue);

        Task StartConsumerAsync(string queueName, Func<object, Task> handler);
        Task StopAllConsumersAsync();

        Task<RabbitMqHealthStatus> GetHealthStatusAsync();
    }

    public class RabbitMqHealthStatus
    {
        public bool IsHealthy { get; set; }
        public bool IsConnected { get; set; }
        public int QueueCount { get; set; }
        public int MessageCount { get; set; }
        public string LastError { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
    }
}