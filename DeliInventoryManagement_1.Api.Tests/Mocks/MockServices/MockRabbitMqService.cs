using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockServices
{
    public class MockRabbitMqService : IRabbitMqService
    {
        private readonly List<string> _publishedMessages = new();
        private readonly List<string> _createdQueues = new();
        private bool _isConnected = false;
        private readonly List<string> _consumers = new();

        // Interface Properties
        public bool IsConnected => _isConnected;

        // Test Helper Properties
        public IReadOnlyList<string> PublishedMessages => _publishedMessages.AsReadOnly();
        public IReadOnlyList<string> CreatedQueues => _createdQueues.AsReadOnly();

        // ============= CONNECTION MANAGEMENT =============
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

        // ============= QUEUE SETUP =============
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

        // ============= SALE EVENTS =============
        public Task PublishSaleCreatedAsync(Sale sale)
        {
            var message = $@"SaleCreated:{sale.Id}:{sale.ProductId}:{sale.Quantity}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishSaleUpdatedAsync(Sale sale)
        {
            var message = $@"SaleUpdated:{sale.Id}:{sale.Quantity}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishSaleCancelledAsync(string saleId, string reason)
        {
            var message = $@"SaleCancelled:{saleId}:{reason}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        // ============= RESTOCK EVENTS =============
        public Task PublishRestockCreatedAsync(Restock restock)
        {
            var message = $@"RestockCreated:{restock.Id}:{restock.Id}:{restock.Quantity}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishLowStockAlertAsync(Product product, int currentStock, int minimumStock)
        {
            var message = $@"LowStockAlert:{product.Id}:{currentStock}:{minimumStock}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        // ============= INVENTORY EVENTS =============
        public Task PublishProductUpdatedAsync(Product product)
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

        // ============= REPORTING EVENTS =============
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

        // ============= ERROR HANDLING =============
        public Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue)
        {
            var message = $@"DeadLetter:{queue}:{error}";
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishToRetryQueueAsync(object message, int retryCount, string queue)
        {
            var msg = $@"Retry:{queue}:{retryCount}";
            _publishedMessages.Add(msg);
            return Task.CompletedTask;
        }

        // ============= CONSUMER MANAGEMENT =============
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

        // ============= HEALTH CHECK =============
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

        // ============= TEST HELPER METHODS =============
        public void ClearMessages()
        {
            _publishedMessages.Clear();
        }

        public bool WasMessagePublished(string messageContent)
        {
            return _publishedMessages.Any(m => m.Contains(messageContent));
        }

        public int CountMessagesByType(string eventType)
        {
            return _publishedMessages.Count(m => m.StartsWith(eventType));
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