using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
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

            _service = new SupplierService(
                mockCosmosClient.Object,
                cosmosOptions,
                _mockCache.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsSupplier()
        {
            var supplierId = "SUP#123";
            var expectedSupplier = new SupplierV5
            {
                Id = supplierId,
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "Test Supplier",
                Email = "test@supplier.com",
                Phone = "123-456-7890"
            };

            var mockResponse = new Mock<ItemResponse<SupplierV5>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedSupplier);

            _mockContainer.Setup(c => c.ReadItemAsync<SupplierV5>(
                    supplierId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await _service.GetByIdAsync(supplierId);

            Assert.NotNull(result);
            Assert.Equal(supplierId, result.Id);
            Assert.Equal("Test Supplier", result.Name);
        }

        [Fact]
        public async Task CreateAsync_CreatesSupplierSuccessfully()
        {
            var newSupplier = new SupplierV5
            {
                Pk = "STORE#1",
                Type = "Supplier",
                Name = "New Supplier",
                Email = "new@supplier.com",
                Phone = "555-1234"
            };

            var mockResponse = new Mock<ItemResponse<SupplierV5>>();
            mockResponse.Setup(r => r.Resource).Returns(new SupplierV5
            {
                Id = "SUP#new123",
                Pk = "STORE#1",
                Type = "Supplier",
                Name = newSupplier.Name,
                Email = newSupplier.Email,
                Phone = newSupplier.Phone
            });

            _mockContainer.Setup(c => c.CreateItemAsync(
                    It.IsAny<SupplierV5>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var result = await _service.CreateAsync(newSupplier);

            Assert.NotNull(result);
            Assert.Equal("New Supplier", result.Name);
            Assert.Equal("new@supplier.com", result.Email);
        }
    }
}