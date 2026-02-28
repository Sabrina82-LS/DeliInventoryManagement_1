using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DeliInventoryManagement_1.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DeliInventoryManagement_1.Api.Services
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqService> _logger;
        private IConnection _connection;
        private IModel _channel;
        private bool _isConnected;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConnected => _isConnected;

        public async Task ConnectAsync()
        {
            try
            {
                _logger.LogInformation("Connecting to RabbitMQ...");

                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:Username"] ?? "admin",
                    Password = _configuration["RabbitMQ:Password"] ?? "admin123",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _isConnected = true;

                _logger.LogInformation("Connected to RabbitMQ");
                await CreateQueuesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _isConnected = false;
                _logger.LogInformation("Disconnected from RabbitMQ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting");
            }
            await Task.CompletedTask;
        }

        public async Task CreateQueuesAsync()
        {
            if (!_isConnected) await ConnectAsync();

            _channel.QueueDeclare("sale.created", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("restock.created", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("sale.created.retry", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("restock.created.retry", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("sale.created.dlq", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("restock.created.dlq", durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("Queues created");
            await Task.CompletedTask;
        }

        public async Task PublishSaleCreatedAsync(Sale sale)
        {
            var message = JsonSerializer.Serialize(new { EventType = "SaleCreated", SaleId = sale.Id });
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", "sale.created", null, body);
            _logger.LogInformation("Published sale {SaleId}", sale.Id);
            await Task.CompletedTask;
        }

        public async Task PublishRestockCreatedAsync(Restock restock)
        {
            var message = JsonSerializer.Serialize(new { EventType = "RestockCreated", RestockId = restock.Id });
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", "restock.created", null, body);
            _logger.LogInformation("Published restock {RestockId}", restock.Id);
            await Task.CompletedTask;
        }

        public async Task PublishProductUpdatedAsync(Product product)
        {
            var message = JsonSerializer.Serialize(new { EventType = "ProductUpdated", ProductId = product.Id });
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", "product.updated", null, body);
            await Task.CompletedTask;
        }

        public async Task PublishProductDiscontinuedAsync(string productId)
        {
            var message = JsonSerializer.Serialize(new { EventType = "ProductDiscontinued", ProductId = productId });
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", "product.discontinued", null, body);
            await Task.CompletedTask;
        }

        public async Task PublishLowStockAlertAsync(Product product, int currentStock, int minimumStock)
        {
            var message = JsonSerializer.Serialize(new { EventType = "LowStockAlert", ProductId = product.Id });
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish("", "low.stock.alert", null, body);
            await Task.CompletedTask;
        }

        public Task PublishSaleUpdatedAsync(Sale sale) => Task.CompletedTask;
        public Task PublishSaleCancelledAsync(string saleId, string reason) => Task.CompletedTask;
        public Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue) => Task.CompletedTask;
        public Task PublishToRetryQueueAsync(object message, int retryCount, string queue) => Task.CompletedTask;
        public Task StartConsumerAsync(string queueName, Func<object, Task> handler) => Task.CompletedTask;
        public Task StopAllConsumersAsync() => Task.CompletedTask;

        public async Task<RabbitMqHealthStatus> GetHealthStatusAsync()
        {
            return await Task.FromResult(new RabbitMqHealthStatus { IsConnected = _isConnected });
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}