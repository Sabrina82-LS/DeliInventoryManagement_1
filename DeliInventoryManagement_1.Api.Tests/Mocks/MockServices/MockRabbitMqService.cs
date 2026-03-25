using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockServices
{
    public class MockRabbitMqService : IRabbitMqService
    {
        private readonly List<string> _publishedMessages = new();
        private readonly List<string> _createdQueues = new();
        private readonly List<string> _consumers = new();
        private bool _isConnected = false;

        public bool IsConnected => _isConnected;

        public IReadOnlyList<string> PublishedMessages => _publishedMessages.AsReadOnly();
        public IReadOnlyList<string> CreatedQueues => _createdQueues.AsReadOnly();

        public Task ConnectAsync()
        {
            _isConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            _isConnected = false;
            _consumers.Clear();
            return Task.CompletedTask;
        }

        public Task CreateQueuesAsync()
        {
            _createdQueues.AddRange(new[]
            {
                "sale.created",
                "restock.created",
                "sale.created.retry",
                "restock.created.retry",
                "sale.created.dlq",
                "restock.created.dlq"
            });

            return Task.CompletedTask;
        }

        public Task PublishSaleCreatedAsync(SaleV5 sale)
        {
            var message = $@"SaleCreated:{sale.Id}:{sale.Total}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishSaleUpdatedAsync(SaleV5 sale)
        {
            var message = $@"SaleUpdated:{sale.Id}:{sale.Total}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishSaleCancelledAsync(string saleId, string reason)
        {
            var message = $@"SaleCancelled:{saleId}:{reason}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishRestockCreatedAsync(RestockV5 restock)
        {
            var message = $@"RestockCreated:{restock.Id}:{restock.Total}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishLowStockAlertAsync(ProductV5 product, int currentStock, int minimumStock)
        {
            var message = $@"LowStockAlert:{product.Id}:{currentStock}:{minimumStock}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishProductUpdatedAsync(ProductV5 product)
        {
            var message = $@"ProductUpdated:{product.Id}:{product.Name}:{product.Quantity}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishProductDiscontinuedAsync(string productId)
        {
            var message = $@"ProductDiscontinued:{productId}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishSalesReportRequestedAsync(DateTime startDate, DateTime endDate, string requestedBy)
        {
            var message = $@"SalesReportRequested:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{requestedBy}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishReportGeneratedAsync(string reportId, string reportUrl)
        {
            var message = $@"ReportGenerated:{reportId}:{reportUrl}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue)
        {
            var message = $@"DeadLetter:{queue}:{error}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishToRetryQueueAsync(object message, int retryCount, string queue)
        {
            var retryMessage = $@"Retry:{queue}:{retryCount}";
            _publishedMessages.Add(retryMessage);
            return Task.CompletedTask;
        }

        public Task StartConsumerAsync(string queueName, Func<object, Task> handler)
        {
            _consumers.Add(queueName);
            return Task.CompletedTask;
        }

        public Task StopAllConsumersAsync()
        {
            _consumers.Clear();
            return Task.CompletedTask;
        }

        public Task<RabbitMqHealthStatus> GetHealthStatusAsync()
        {
            var status = new RabbitMqHealthStatus
            {
                IsHealthy = _isConnected,
                IsConnected = _isConnected,
                QueueCount = _createdQueues.Count,
                MessageCount = _publishedMessages.Count,
                LastChecked = DateTime.UtcNow
            };

            return Task.FromResult(status);
        }

        public void ClearMessages()
        {
            _publishedMessages.Clear();
        }

        public bool WasMessagePublished(string messageContent)
        {
            return _publishedMessages.Any(m =>
                m.Contains(messageContent, StringComparison.OrdinalIgnoreCase));
        }

        public int CountMessagesByType(string eventType)
        {
            return _publishedMessages.Count(m =>
                m.StartsWith(eventType, StringComparison.OrdinalIgnoreCase));
        }

        public void Reset()
        {
            _publishedMessages.Clear();
            _createdQueues.Clear();
            _consumers.Clear();
            _isConnected = false;
        }
    }
}