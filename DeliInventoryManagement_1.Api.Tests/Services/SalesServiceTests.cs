using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.ModelsV5.Line;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Services.IService;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Services
{
    public class SalesServiceTests
    {
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IRabbitMqService> _mockRabbitMq;
        private readonly Mock<ILogger<SalesService>> _mockLogger;
        private readonly SalesService _service;

        public SalesServiceTests()
        {
            _mockContainer = new Mock<Container>();
            _mockProductService = new Mock<IProductService>();
            _mockRabbitMq = new Mock<IRabbitMqService>();
            _mockLogger = new Mock<ILogger<SalesService>>();

            var mockCosmosClient = new Mock<CosmosClient>();

            var cosmosOptions = Options.Create(new CosmosOptions
            {
                DatabaseId = "TestDb",
                DefaultStorePk = "STORE#1",
                Containers = new CosmosContainers
                {
                    Products = "Products",
                    Suppliers = "Suppliers",
                    ReorderRules = "ReorderRules",
                    Operations = "Operations",
                    Users = "Users"
                }
            });

            mockCosmosClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            _service = new SalesService(
                mockCosmosClient.Object,
                cosmosOptions,
                _mockProductService.Object,
                _mockRabbitMq.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateSaleAsync_WithValidStock_CreatesSale()
        {
            var productId = "PROD#123";

            var product = new ProductV5
            {
                Id = productId,
                Pk = "STORE#1",
                Type = "Product",
                Name = "Test Product",
                Quantity = 10,
                Price = 15.99m
            };

            var sale = new SaleV5
            {
                Pk = "STORE#1",
                Type = "Sale",
                Lines = new List<SaleLineV5>
                {
                    new SaleLineV5
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 3,
                        UnitPrice = 15.99m
                    }
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _mockProductService.Setup(s => s.UpdateAsync(productId, It.IsAny<ProductV5>()))
                .ReturnsAsync((string id, ProductV5 p) => p);

            var createdSale = new SaleV5
            {
                Id = "SALE#123",
                Pk = "STORE#1",
                Type = "Sale",
                Lines = sale.Lines,
                Total = 3 * product.Price,
                Date = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var mockResponse = new Mock<ItemResponse<SaleV5>>();
            mockResponse.Setup(r => r.Resource).Returns(createdSale);

            _mockContainer.Setup(c => c.CreateItemAsync(
                    It.IsAny<SaleV5>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await _service.CreateSaleAsync(sale);

            Assert.NotNull(result);
            Assert.Equal("SALE#123", result.Id);
            Assert.Equal(3 * product.Price, result.Total);

            _mockProductService.Verify(s => s.UpdateAsync(
                productId,
                It.Is<ProductV5>(p => p.Quantity == 7)),
                Times.Once);

            _mockRabbitMq.Verify(r => r.PublishSaleCreatedAsync(It.IsAny<SaleV5>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleAsync_WithInsufficientStock_ThrowsException()
        {
            var productId = "PROD#123";

            var product = new ProductV5
            {
                Id = productId,
                Pk = "STORE#1",
                Type = "Product",
                Name = "Test Product",
                Quantity = 2,
                Price = 15.99m
            };

            var sale = new SaleV5
            {
                Pk = "STORE#1",
                Type = "Sale",
                Lines = new List<SaleLineV5>
                {
                    new SaleLineV5
                    {
                        ProductId = productId,
                        ProductName = "Test Product",
                        Quantity = 5,
                        UnitPrice = 15.99m
                    }
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(product);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateSaleAsync(sale));

            _mockProductService.Verify(s => s.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<ProductV5>()),
                Times.Never);

            _mockRabbitMq.Verify(r => r.PublishSaleCreatedAsync(It.IsAny<SaleV5>()), Times.Never);
        }

        [Fact]
        public async Task CreateSaleAsync_WithNonExistentProduct_ThrowsException()
        {
            var productId = "PROD#999";

            var sale = new SaleV5
            {
                Pk = "STORE#1",
                Type = "Sale",
                Lines = new List<SaleLineV5>
                {
                    new SaleLineV5
                    {
                        ProductId = productId,
                        ProductName = "Missing Product",
                        Quantity = 1,
                        UnitPrice = 10m
                    }
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync((ProductV5?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateSaleAsync(sale));
        }

        [Fact]
        public async Task GetSaleByIdAsync_WithValidId_ReturnsSale()
        {
            var saleId = "SALE#123";

            var expectedSale = new SaleV5
            {
                Id = saleId,
                Pk = "STORE#1",
                Type = "Sale",
                Total = 100m,
                Date = DateTime.UtcNow,
                Lines = new List<SaleLineV5>()
            };

            var mockResponse = new Mock<ItemResponse<SaleV5>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedSale);

            _mockContainer.Setup(c => c.ReadItemAsync<SaleV5>(
                    saleId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await _service.GetSaleByIdAsync(saleId);

            Assert.NotNull(result);
            Assert.Equal(saleId, result.Id);
            Assert.Equal(100m, result.Total);
        }

        [Fact]
        public async Task GetSaleByIdAsync_WithInvalidId_ReturnsNull()
        {
            var saleId = "invalid-id";

            _mockContainer.Setup(c => c.ReadItemAsync<SaleV5>(
                    saleId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            var result = await _service.GetSaleByIdAsync(saleId);

            Assert.Null(result);
        }
    }
}