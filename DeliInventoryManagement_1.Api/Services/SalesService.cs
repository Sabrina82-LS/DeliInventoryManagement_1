using DeliInventoryManagement_1.Api.Models;
using Microsoft.Azure.Cosmos;


namespace DeliInventoryManagement_1.Api.Services
{
    public class SalesService : ISalesService
    {
        private readonly Container _container;
        private readonly IProductService _productService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<SalesService> _logger;
        private const string TypeValue = "Sale";

        public SalesService(
            CosmosClient cosmosClient,
            IConfiguration config,
            IProductService productService,
            IRabbitMqService rabbitMqService,
            ILogger<SalesService> logger)
        {
            _productService = productService;
            _rabbitMqService = rabbitMqService;
            _logger = logger;

            var cosmosSection = config.GetSection("CosmosDb");
            var databaseId = cosmosSection["DatabaseId"]!;
            var containerId = cosmosSection["ContainerId"]!;
            _container = cosmosClient.GetContainer(databaseId, containerId);
        }

        public async Task<Sales?> GetSaleByIdAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Sales>(
                    id,
                    new PartitionKey(TypeValue));

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

        public async Task<IEnumerable<Sales>> GetAllSalesAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Type = @type ORDER BY c.CreatedAtUtc DESC")
                .WithParameter("@type", TypeValue);

            var iterator = _container.GetItemQueryIterator<Sales>(query);
            var results = new List<Sales>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<IEnumerable<Sales>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Type = @type AND c.Date >= @startDate AND c.Date <= @endDate ORDER BY c.Date DESC")
                .WithParameter("@type", TypeValue)
                .WithParameter("@startDate", startDate)
                .WithParameter("@endDate", endDate);

            var iterator = _container.GetItemQueryIterator<Sales>(query);
            var results = new List<Sales>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<Sales> CreateSaleAsync(Sales sale)
        {
            // Validate sale has lines
            if (sale.Lines == null || !sale.Lines.Any())
            {
                throw new InvalidOperationException("Sale must have at least one line");
            }

            // Process each line: validate product and check stock
            foreach (var line in sale.Lines)
            {
                var product = await _productService.GetByIdAsync(line.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException($"Product {line.ProductId} not found");
                }

                if (product.Quantity < line.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {product.Name}. " +
                        $"Available: {product.Quantity}, Requested: {line.Quantity}");
                }

                // Set product details
                line.ProductName = product.Name;
                line.UnitPrice = product.Price;

                // Update product quantity (decrease stock)
                product.Quantity -= line.Quantity;
                await _productService.UpdateAsync(product.Id, product);
            }

            // Calculate total
            sale.Total = sale.Lines.Sum(l => l.Quantity * l.UnitPrice);

            // Set metadata
            sale.Type = TypeValue;
            sale.CreatedAtUtc = DateTime.UtcNow;
            sale.Date = DateTime.UtcNow;

            if (string.IsNullOrEmpty(sale.Id))
            {
                sale.Id = Guid.NewGuid().ToString();
            }

            // Save the sale
            var response = await _container.CreateItemAsync(sale, new PartitionKey(TypeValue));

            _logger.LogInformation("Sale created with ID: {SaleId}, Total: {Total}", sale.Id, sale.Total);

            // Publish RabbitMQ event (if needed)
            try
            {
                // Create a Sale event object (different from Sales entity)
                var saleEvent = new Sale
                {
                    Id = sale.Id,
                    ProductId = sale.Lines.First().ProductId,
                    ProductName = sale.Lines.First().ProductName,
                    Quantity = sale.Lines.Sum(l => l.Quantity),
                    UnitPrice = sale.Lines.First().UnitPrice,
                    Total = sale.Total,
                    CreatedAtUtc = sale.CreatedAtUtc
                };

                await _rabbitMqService.PublishSaleCreatedAsync(saleEvent);
                _logger.LogInformation("RabbitMQ event published for sale {SaleId}", sale.Id);
            }
            catch (Exception ex)
            {
                // Log but don't fail the sale if RabbitMQ is down
                _logger.LogWarning(ex, "Failed to publish RabbitMQ event for sale {SaleId}", sale.Id);
            }

            return response.Resource;
        }
    }
}