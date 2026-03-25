using DeliInventoryManagement_1.Api;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class SimpleProductEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public SimpleProductEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Fact(Skip = "Skipped in CI - depends on Cosmos")]
        public async Task CreateProduct_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();

            var productRequest = new
            {
                name = "Test Product",
                categoryId = "c1",
                categoryName = "Test Category",
                quantity = 10,
                cost = 5.99m,
                price = 10.99m,
                reorderLevel = 5,
                isActive = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v5/products", productRequest);

            // Assert
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var product = JsonSerializer.Deserialize<ProductV5>(content, _jsonOptions);

            Assert.NotNull(product);
            Assert.False(string.IsNullOrWhiteSpace(product!.Id));
            Assert.Equal("Test Product", product.Name);
        }
    }
}