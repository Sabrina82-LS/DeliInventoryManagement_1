using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class SimpleProductEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public SimpleProductEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Fact]
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
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<ProductV5>(content, _jsonOptions);

            Assert.NotNull(product);
            Assert.NotNull(product.Id);
            Assert.Equal("Test Product", product.Name);
        }
    }
}