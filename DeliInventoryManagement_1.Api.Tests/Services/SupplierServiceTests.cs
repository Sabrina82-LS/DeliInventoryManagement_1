using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Services
{
    public class SupplierServiceTests
    {
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly SupplierService _service;

        public SupplierServiceTests()
        {
            _mockContainer = new Mock<Container>();
            _mockCache = new Mock<IMemoryCache>();

            var mockCosmosClient = new Mock<CosmosClient>();
            var mockConfig = new Mock<IConfiguration>();

            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x["DatabaseId"]).Returns("TestDb");
            mockConfigSection.Setup(x => x["ContainerId"]).Returns("TestContainer");
            mockConfig.Setup(x => x.GetSection("CosmosDb")).Returns(mockConfigSection.Object);

            mockCosmosClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            _service = new SupplierService(mockCosmosClient.Object, mockConfig.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsSupplier()
        {
            // Arrange
            var supplierId = "SUP#123";
            var expectedSupplier = new Supplier
            {
                Id = supplierId,
                Name = "Test Supplier",
                Email = "test@supplier.com",
                Phone = "123-456-7890"
            };

            var mockResponse = new Mock<ItemResponse<Supplier>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedSupplier);

            _mockContainer.Setup(c => c.ReadItemAsync<Supplier>(
                    supplierId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.GetByIdAsync(supplierId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(supplierId, result.Id);
            Assert.Equal("Test Supplier", result.Name);
        }

        [Fact]
        public async Task CreateAsync_CreatesSupplierSuccessfully()
        {
            // Arrange
            var newSupplier = new Supplier
            {
                Name = "New Supplier",
                Email = "new@supplier.com",
                Phone = "555-1234"
            };

            var mockResponse = new Mock<ItemResponse<Supplier>>();
            mockResponse.Setup(r => r.Resource).Returns(() =>
            {
                var result = new Supplier
                {
                    Id = "SUP#new123",
                    Name = newSupplier.Name,
                    Email = newSupplier.Email,
                    Phone = newSupplier.Phone
                };
                return result;
            });

            _mockContainer.Setup(c => c.CreateItemAsync(
                    It.IsAny<Supplier>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.CreateAsync(newSupplier);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Supplier", result.Name);
            Assert.Equal("new@supplier.com", result.Email);
        }
    }
}