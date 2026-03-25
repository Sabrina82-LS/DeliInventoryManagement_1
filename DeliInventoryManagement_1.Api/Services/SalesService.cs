using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services.IService;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Services;

public class SalesService : ISalesService
{
    private readonly Container _container;
    private readonly IProductService _productService;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<SalesService> _logger;
    private readonly string _storePk;

    public SalesService(
        CosmosClient cosmosClient,
        IOptions<CosmosOptions> options,
        IProductService productService,
        IRabbitMqService rabbitMqService,
        ILogger<SalesService> logger)
    {
        _productService = productService;
        _rabbitMqService = rabbitMqService;
        _logger = logger;

        var opt = options.Value;
        _storePk = opt.DefaultStorePk;

        _container = cosmosClient.GetContainer(opt.DatabaseId, opt.Containers.Operations);
    }

    public async Task<SaleV5?> GetSaleByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<SaleV5>(
                id,
                new PartitionKey(_storePk));

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale by ID {SaleId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<SaleV5>> GetAllSalesAsync()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk AND c.type = @type ORDER BY c.createdAtUtc DESC")
            .WithParameter("@pk", _storePk)
            .WithParameter("@type", "Sale");

        var iterator = _container.GetItemQueryIterator<SaleV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(_storePk)
            });

        var results = new List<SaleV5>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IEnumerable<SaleV5>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.pk = @pk AND c.type = @type AND c.date >= @startDate AND c.date <= @endDate ORDER BY c.date DESC")
            .WithParameter("@pk", _storePk)
            .WithParameter("@type", "Sale")
            .WithParameter("@startDate", startDate)
            .WithParameter("@endDate", endDate);

        var iterator = _container.GetItemQueryIterator<SaleV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(_storePk)
            });

        var results = new List<SaleV5>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<SaleV5> CreateSaleAsync(SaleV5 sale)
    {
        if (sale.Lines is null || !sale.Lines.Any())
            throw new InvalidOperationException("Sale must have at least one line.");

        foreach (var line in sale.Lines)
        {
            var product = await _productService.GetByIdAsync(line.ProductId);

            if (product is null)
                throw new InvalidOperationException($"Product {line.ProductId} not found.");

            if (product.Quantity < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product {product.Name}. Available: {product.Quantity}, Requested: {line.Quantity}");
            }

            line.ProductName = product.Name;
            line.UnitPrice = product.Price;

            product.Quantity -= line.Quantity;
            await _productService.UpdateAsync(product.Id, product);
        }

        sale.Total = sale.Lines.Sum(l => l.Quantity * l.UnitPrice);

        if (string.IsNullOrWhiteSpace(sale.Id))
            sale.Id = Guid.NewGuid().ToString("n");

        sale.Pk = _storePk;
        sale.Type = "Sale";
        sale.Date = DateTime.UtcNow;
        sale.CreatedAtUtc = DateTime.UtcNow;
        sale.UpdatedAtUtc = DateTime.UtcNow;

        var response = await _container.CreateItemAsync(sale, new PartitionKey(sale.Pk));

        _logger.LogInformation("Sale created with ID: {SaleId}, Total: {Total}", sale.Id, sale.Total);

        try
        {
            await _rabbitMqService.PublishSaleCreatedAsync(response.Resource);
            _logger.LogInformation("RabbitMQ event published for sale {SaleId}", sale.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish RabbitMQ event for sale {SaleId}", sale.Id);
        }

        return response.Resource;
    }
}