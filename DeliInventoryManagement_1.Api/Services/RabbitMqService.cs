using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace DeliInventoryManagement_1.Api.Services;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqService> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _isConnected;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync()
    {
        if (_isConnected && _connection is not null && _channel is not null)
            return;

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
            if (_connection is not null && _connection.IsOpen)
            {
                _connection.Close();
                await Task.Delay(100);
            }
        }
        catch (AlreadyClosedException)
        {
            _logger.LogInformation("RabbitMQ connection already closed");
        }
        catch (ObjectDisposedException)
        {
            _logger.LogInformation("RabbitMQ connection already disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from RabbitMQ");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
            _isConnected = false;
        }
    }

    public async Task CreateQueuesAsync()
    {
        if (!_isConnected)
            await ConnectAsync();

        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not available.");

        _channel.QueueDeclare("sale.created", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("restock.created", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("sale.created.retry", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("restock.created.retry", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("sale.created.dlq", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("restock.created.dlq", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("product.updated", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("product.discontinued", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("low.stock.alert", durable: true, exclusive: false, autoDelete: false);

        _logger.LogInformation("RabbitMQ queues declared");
    }

    public async Task PublishSaleCreatedAsync(SaleV5 sale)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "SaleCreated",
            SaleId = sale.Id,
            Date = sale.Date,
            Total = sale.Total
        });

        Publish("sale.created", message);
        _logger.LogInformation("Published SaleCreated for sale {SaleId}", sale.Id);
    }

    public async Task PublishSaleUpdatedAsync(SaleV5 sale)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "SaleUpdated",
            SaleId = sale.Id,
            Date = sale.Date,
            Total = sale.Total
        });

        Publish("sale.created.retry", message);
        _logger.LogInformation("Published SaleUpdated for sale {SaleId}", sale.Id);
    }

    public async Task PublishSaleCancelledAsync(string saleId, string reason)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "SaleCancelled",
            SaleId = saleId,
            Reason = reason
        });

        Publish("sale.created.dlq", message);
        _logger.LogInformation("Published SaleCancelled for sale {SaleId}", saleId);
    }

    public async Task PublishRestockCreatedAsync(RestockV5 restock)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "RestockCreated",
            RestockId = restock.Id,
            SupplierId = restock.SupplierId,
            SupplierName = restock.SupplierName,
            Date = restock.Date,
            Total = restock.Total
        });

        Publish("restock.created", message);
        _logger.LogInformation("Published RestockCreated for restock {RestockId}", restock.Id);
    }

    public async Task PublishProductUpdatedAsync(ProductV5 product)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "ProductUpdated",
            ProductId = product.Id,
            Name = product.Name,
            Quantity = product.Quantity,
            Price = product.Price
        });

        Publish("product.updated", message);
        _logger.LogInformation("Published ProductUpdated for product {ProductId}", product.Id);
    }

    public async Task PublishProductDiscontinuedAsync(string productId)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "ProductDiscontinued",
            ProductId = productId
        });

        Publish("product.discontinued", message);
        _logger.LogInformation("Published ProductDiscontinued for product {ProductId}", productId);
    }

    public async Task PublishLowStockAlertAsync(ProductV5 product, int currentStock, int minimumStock)
    {
        await EnsureConnectedAsync();

        var message = JsonSerializer.Serialize(new
        {
            EventType = "LowStockAlert",
            ProductId = product.Id,
            ProductName = product.Name,
            CurrentStock = currentStock,
            MinimumStock = minimumStock
        });

        Publish("low.stock.alert", message);
        _logger.LogInformation("Published LowStockAlert for product {ProductId}", product.Id);
    }

    public Task PublishToDeadLetterQueueAsync(object originalMessage, string error, string queue)
    {
        _logger.LogWarning("DLQ publish requested for queue {Queue}. Error: {Error}", queue, error);
        return Task.CompletedTask;
    }

    public Task PublishToRetryQueueAsync(object message, int retryCount, string queue)
    {
        _logger.LogWarning("Retry publish requested for queue {Queue}. Retry count: {RetryCount}", queue, retryCount);
        return Task.CompletedTask;
    }

    public Task StartConsumerAsync(string queueName, Func<object, Task> handler)
    {
        _logger.LogInformation("StartConsumerAsync called for queue {QueueName}, but no generic consumer is configured.", queueName);
        return Task.CompletedTask;
    }

    public Task StopAllConsumersAsync()
    {
        _logger.LogInformation("StopAllConsumersAsync called.");
        return Task.CompletedTask;
    }

    public Task<RabbitMqHealthStatus> GetHealthStatusAsync()
    {
        return Task.FromResult(new RabbitMqHealthStatus
        {
            IsHealthy = _isConnected,
            IsConnected = _isConnected,
            QueueCount = 0,
            MessageCount = 0,
            LastError = string.Empty,
            LastChecked = DateTime.UtcNow
        });
    }

    private async Task EnsureConnectedAsync()
    {
        if (!_isConnected || _channel is null)
            await ConnectAsync();

        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not available.");
    }

    private void Publish(string queueName, string message)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not available.");

        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}