using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class CosmosDbDiagnosticTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public CosmosDbDiagnosticTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Fact]
        public async Task Test1_CreateAndReadProduct()
        {
            // Arrange
            var client = _factory.CreateClient();

            Console.WriteLine("\n=== TEST 1: Create and Read Product ===");

            // 1. CREATE PRODUCT
            Console.WriteLine("Creating product...");
            var createResponse = await client.PostAsJsonAsync("/api/v5/products", new
            {
                name = "Test Product",
                categoryId = "c1",
                categoryName = "Test Category",
                quantity = 10,
                cost = 5.99m,
                price = 10.99m,
                reorderLevel = 5,
                isActive = true
            });

            Console.WriteLine($"Create Response: {createResponse.StatusCode}");
            var createContent = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Create Content: {createContent}");

            // 2. EXTRACT ID
            var productDict = JsonSerializer.Deserialize<Dictionary<string, object>>(createContent, _jsonOptions);
            string productId = productDict?["id"]?.ToString() ?? "";
            Console.WriteLine($"Product ID: {productId}");

            // 3. TRY TO READ
            Console.WriteLine("\nReading product...");
            var readResponse = await client.GetAsync($"/api/v5/products/{productId}");
            Console.WriteLine($"Read Response: {readResponse.StatusCode}");

            if (readResponse.StatusCode == HttpStatusCode.OK)
            {
                var readContent = await readResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Read Content: {readContent}");
            }

            // This test is just for information - won't fail
            Assert.True(true);
        }
    }
}