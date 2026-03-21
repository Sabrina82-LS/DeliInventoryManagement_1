using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DeliInventoryManagement_1.Api.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _mockContainer = new Mock<Container>();
            _mockCache = new Mock<IMemoryCache>();

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

            _service = new ProductService(mockCosmosClient.Object, mockConfig.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsProduct()
        {
            // Arrange
            var productId = "PROD#123";
            var expectedProduct = new Product
            {
                Id = productId,
                Name = "Test Product",
                Type = "Product",
                Quantity = 10,
                Price = 15.99m
            };

            var mockResponse = new Mock<ItemResponse<Product>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedProduct);

            _mockContainer.Setup(c => c.ReadItemAsync<Product>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.GetByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Test Product", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var productId = "invalid-id";

            _mockContainer.Setup(c => c.ReadItemAsync<Product>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _service.GetByIdAsync(productId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_CreatesProductSuccessfully()
        {
            // Arrange
            var newProduct = new Product
            {
                Name = "New Product",
                Price = 19.99m,
                Quantity = 5,
                CategoryId = "cat1",
                CategoryName = "Category 1"
            };

            var mockResponse = new Mock<ItemResponse<Product>>();
            mockResponse.Setup(r => r.Resource).Returns(() =>
            {
                return new Product
                {
                    Id = "PROD#new123",
                    Name = newProduct.Name,
                    Price = newProduct.Price,
                    Quantity = newProduct.Quantity,
                    CategoryId = newProduct.CategoryId,
                    CategoryName = newProduct.CategoryName
                };
            });

            _mockContainer.Setup(c => c.CreateItemAsync(
                    It.IsAny<Product>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.CreateAsync(newProduct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Product", result.Name);
            Assert.Equal(19.99m, result.Price);
            _mockContainer.Verify(c => c.CreateItemAsync(
                It.IsAny<Product>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidId_UpdatesProduct()
        {
            // Arrange
            var productId = "PROD#123";
            var existingProduct = new Product
            {
                Id = productId,
                Name = "Old Name",
                Price = 10m,
                Quantity = 5
            };

            var updatedProduct = new Product
            {
                Name = "New Name",
                Price = 20m,
                Quantity = 10
            };

            // Mock GetByIdAsync
            var mockReadResponse = new Mock<ItemResponse<Product>>();
            mockReadResponse.Setup(r => r.Resource).Returns(existingProduct);

            _mockContainer.Setup(c => c.ReadItemAsync<Product>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReadResponse.Object);

            // Mock ReplaceItemAsync
            var mockReplaceResponse = new Mock<ItemResponse<Product>>();
            mockReplaceResponse.Setup(r => r.Resource).Returns(existingProduct);

            _mockContainer.Setup(c => c.ReplaceItemAsync(
                    It.IsAny<Product>(),
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReplaceResponse.Object);

            // Act
            var result = await _service.UpdateAsync(productId, updatedProduct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            Assert.Equal(20m, result.Price);
            Assert.Equal(10, result.Quantity);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var productId = "PROD#123";

            var mockResponse = new Mock<ItemResponse<Product>>();
            _mockContainer.Setup(c => c.DeleteItemAsync<Product>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.DeleteAsync(productId);

            // Assert
            Assert.True(result);
            _mockContainer.Verify(c => c.DeleteItemAsync<Product>(
                productId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var productId = "invalid-id";

            _mockContainer.Setup(c => c.DeleteItemAsync<Product>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _service.DeleteAsync(productId);

            // Assert
            Assert.False(result);
        }
    }
}