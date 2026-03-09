using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;


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
            var mockConfig = new Mock<IConfiguration>();

            // Setup config
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x["DatabaseId"]).Returns("TestDb");
            mockConfigSection.Setup(x => x["ContainerId"]).Returns("TestContainer");
            mockConfig.Setup(x => x.GetSection("CosmosDb")).Returns(mockConfigSection.Object);

            // Setup CosmosClient to return mock container
            mockCosmosClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            _service = new SalesService(
                mockCosmosClient.Object,
                mockConfig.Object,
                _mockProductService.Object,
                _mockRabbitMq.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateSaleAsync_WithValidStock_CreatesSale()
        {
            // Arrange
            var productId = "PROD#123";
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Quantity = 10,
                Price = 15.99m
            };

            var sale = new Sales
            {
                Lines = new List<SaleLine>
                {
                    new SaleLine { ProductId = productId, Quantity = 3 }
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _mockProductService.Setup(s => s.UpdateAsync(productId, It.IsAny<Product>()))
                .ReturnsAsync((string id, Product p) => p);

            var mockResponse = new Mock<ItemResponse<Sales>>();
            var createdSale = new Sales
            {
                Id = "SALE#123",
                Lines = sale.Lines,
                Total = 3 * product.Price,
                Date = DateTime.UtcNow
            };
            mockResponse.Setup(r => r.Resource).Returns(createdSale);

            _mockContainer.Setup(c => c.CreateItemAsync(
                    It.IsAny<Sales>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.CreateSaleAsync(sale);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SALE#123", result.Id);
            Assert.Equal(3 * product.Price, result.Total);

            // Verify product was updated (stock decreased)
            _mockProductService.Verify(s => s.UpdateAsync(
                productId,
                It.Is<Product>(p => p.Quantity == 7)), // 10 - 3 = 7
                Times.Once);

            // Verify RabbitMQ event was published
            _mockRabbitMq.Verify(r => r.PublishSaleCreatedAsync(It.IsAny<Sale>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleAsync_WithInsufficientStock_ThrowsException()
        {
            // Arrange
            var productId = "PROD#123";
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Quantity = 2, // Only 2 in stock
                Price = 15.99m
            };

            var sale = new Sales
            {
                Lines = new List<SaleLine>
                {
                    new SaleLine { ProductId = productId, Quantity = 5 } // Trying to sell 5
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync(product);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateSaleAsync(sale));

            // Verify product was NOT updated
            _mockProductService.Verify(s => s.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<Product>()),
                Times.Never);

            // Verify RabbitMQ event was NOT published
            _mockRabbitMq.Verify(r => r.PublishSaleCreatedAsync(It.IsAny<Sale>()), Times.Never);
        }

        [Fact]
        public async Task CreateSaleAsync_WithNonExistentProduct_ThrowsException()
        {
            // Arrange
            var productId = "PROD#999";
            var sale = new Sales
            {
                Lines = new List<SaleLine>
                {
                    new SaleLine { ProductId = productId, Quantity = 1 }
                }
            };

            _mockProductService.Setup(s => s.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateSaleAsync(sale));
        }

        [Fact]
        public async Task GetSaleByIdAsync_WithValidId_ReturnsSale()
        {
            // Arrange
            var saleId = "SALE#123";
            var expectedSale = new Sales
            {
                Id = saleId,
                Total = 100m,
                Date = DateTime.UtcNow,
                Lines = new List<SaleLine>()
            };

            var mockResponse = new Mock<ItemResponse<Sales>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedSale);

            _mockContainer.Setup(c => c.ReadItemAsync<Sales>(
                    saleId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.GetSaleByIdAsync(saleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(saleId, result.Id);
            Assert.Equal(100m, result.Total);
        }

        [Fact]
        public async Task GetSaleByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var saleId = "invalid-id";

            _mockContainer.Setup(c => c.ReadItemAsync<Sales>(
                    saleId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _service.GetSaleByIdAsync(saleId);

            // Assert
            Assert.Null(result);
        }
    }
}