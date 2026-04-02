//using DeliInventoryManagement_1.Api.Configuration;
//using Microsoft.Extensions.Options;

//var cosmosOptions = Options.Create(new CosmosOptions
//{
//    DatabaseId = "TestDb",
//    DefaultStorePk = "STORE#1",
//    Containers = new CosmosContainers
//    {
//        Products = "Products",
//        Suppliers = "Suppliers",
//        ReorderRules = "ReorderRules",
//        Operations = "Operations",
//        Users = "Users"
//    }
//});

using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

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

            mockCosmosClient
                .Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            // IMemoryCache requires TryGetValue to return false (cache miss)
            // so the service always goes to Cosmos in tests
            object? cacheOut = null;
            _mockCache
                .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheOut))
                .Returns(false);

            // CreateEntry is called when the service sets a cache value — we stub it
            _mockCache
                .Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns(Mock.Of<ICacheEntry>());

            _service = new ProductService(
                mockCosmosClient.Object,
                cosmosOptions,
                _mockCache.Object);
        }

        // ──────────────────────────────────────────────
        // GetByIdAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsProduct()
        {
            // Arrange
            var productId = "PROD#001";
            var expected = new ProductV5
            {
                Id = productId,
                Pk = "STORE#1",
                Type = "Product",
                Name = "Test Product",
                CategoryId = "c1",
                CategoryName = "Test Category",
                Quantity = 10,
                Price = 9.99m,
                Cost = 5.00m
            };

            var mockResponse = new Mock<ItemResponse<ProductV5>>();
            mockResponse.Setup(r => r.Resource).Returns(expected);

            _mockContainer
                .Setup(c => c.ReadItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.GetByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result!.Id);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal(10, result.Quantity);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var productId = "PROD#NOTEXIST";

            _mockContainer
                .Setup(c => c.ReadItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException(
                    "Not found",
                    System.Net.HttpStatusCode.NotFound,
                    0, "", 0));

            // Act
            var result = await _service.GetByIdAsync(productId);

            // Assert
            Assert.Null(result);
        }

        // ──────────────────────────────────────────────
        // CreateAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_WithValidProduct_ReturnsCreatedProduct()
        {
            // Arrange
            var newProduct = new ProductV5
            {
                Pk = "STORE#1",
                Type = "Product",
                Name = "New Product",
                CategoryId = "c1",
                CategoryName = "Test Category",
                Quantity = 20,
                Price = 12.99m,
                Cost = 6.00m,
                ReorderLevel = 5,
                IsActive = true
            };

            var createdProduct = new ProductV5
            {
                Id = "PROD#abc123",
                Pk = "STORE#1",
                Type = "Product",
                Name = newProduct.Name,
                CategoryId = newProduct.CategoryId,
                CategoryName = newProduct.CategoryName,
                Quantity = newProduct.Quantity,
                Price = newProduct.Price,
                Cost = newProduct.Cost,
                ReorderLevel = newProduct.ReorderLevel,
                IsActive = newProduct.IsActive
            };

            var mockResponse = new Mock<ItemResponse<ProductV5>>();
            mockResponse.Setup(r => r.Resource).Returns(createdProduct);

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<ProductV5>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.CreateAsync(newProduct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PROD#abc123", result.Id);
            Assert.Equal("New Product", result.Name);
            Assert.Equal(20, result.Quantity);
        }

        // ──────────────────────────────────────────────
        // UpdateAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_WithValidId_ReturnsUpdatedProduct()
        {
            // Arrange
            var productId = "PROD#001";

            var existing = new ProductV5
            {
                Id = productId,
                Pk = "STORE#1",
                Type = "Product",
                Name = "Old Name",
                Quantity = 10,
                Price = 9.99m,
                Cost = 5.00m,
                CategoryId = "c1",
                CategoryName = "Category",
                IsActive = true
            };

            var updated = new ProductV5
            {
                Id = productId,
                Pk = "STORE#1",
                Type = "Product",
                Name = "Updated Name",
                Quantity = 15,
                Price = 11.99m,
                Cost = 6.00m,
                CategoryId = "c1",
                CategoryName = "Category",
                IsActive = true
            };

            // ReadItemAsync returns existing
            var readResponse = new Mock<ItemResponse<ProductV5>>();
            readResponse.Setup(r => r.Resource).Returns(existing);

            _mockContainer
                .Setup(c => c.ReadItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(readResponse.Object);

            // ReplaceItemAsync returns updated
            var replaceResponse = new Mock<ItemResponse<ProductV5>>();
            replaceResponse.Setup(r => r.Resource).Returns(updated);

            _mockContainer
                .Setup(c => c.ReplaceItemAsync(
                    It.IsAny<ProductV5>(),
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceResponse.Object);

            // Act
            var result = await _service.UpdateAsync(productId, updated);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result!.Name);
            Assert.Equal(15, result.Quantity);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var productId = "PROD#NOTEXIST";

            _mockContainer
                .Setup(c => c.ReadItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException(
                    "Not found",
                    System.Net.HttpStatusCode.NotFound,
                    0, "", 0));

            // Act
            var result = await _service.UpdateAsync(productId, new ProductV5());

            // Assert
            Assert.Null(result);
        }

        // ──────────────────────────────────────────────
        // DeleteAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var productId = "PROD#001";
            var mockResponse = new Mock<ItemResponse<ProductV5>>();

            _mockContainer
                .Setup(c => c.DeleteItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.DeleteAsync(productId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var productId = "PROD#NOTEXIST";

            _mockContainer
                .Setup(c => c.DeleteItemAsync<ProductV5>(
                    productId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException(
                    "Not found",
                    System.Net.HttpStatusCode.NotFound,
                    0, "", 0));

            // Act
            var result = await _service.DeleteAsync(productId);

            // Assert
            Assert.False(result);
        }
    }
}