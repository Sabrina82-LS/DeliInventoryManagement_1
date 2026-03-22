using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class CosmosDbDiagnosticTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public CosmosDbDiagnosticTests(CustomWebApplicationFactory factory)
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

            // Act 1 - Create product
            Console.WriteLine("Creating product...");
            var createResponse = await client.PostAsJsonAsync("/api/v5/products", productRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"Create Response: {createResponse.StatusCode}");
            Console.WriteLine($"Create Content: {createContent}");

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(createContent));

            Dictionary<string, object>? createdProductDict = null;

            try
            {
                createdProductDict = JsonSerializer.Deserialize<Dictionary<string, object>>(createContent, _jsonOptions);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Create response was not valid JSON. Error: {ex.Message}\nResponse body: {createContent}");
            }

            Assert.NotNull(createdProductDict);
            Assert.True(createdProductDict!.ContainsKey("id"), "The create response JSON does not contain an 'id' field.");

            var createdProductId = createdProductDict["id"]?.ToString() ?? string.Empty;

            Assert.False(string.IsNullOrWhiteSpace(createdProductId));

            Console.WriteLine($"Created Product ID: {createdProductId}");

            // Act 2 - Read all products
            Console.WriteLine("\nReading all products...");
            var listResponse = await client.GetAsync("/api/v5/products");
            var listContent = await listResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"List Response: {listResponse.StatusCode}");
            Console.WriteLine($"List Content: {listContent}");

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(listContent));

            List<Dictionary<string, object>>? products = null;

            try
            {
                products = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(listContent, _jsonOptions);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"List response was not valid JSON. Error: {ex.Message}\nResponse body: {listContent}");
            }

            Assert.NotNull(products);
            Assert.NotEmpty(products!);

            var createdProductExists = products!.Any(p =>
                p.ContainsKey("id") &&
                p["id"]?.ToString() == createdProductId);

            Assert.True(createdProductExists, $"The created product with ID '{createdProductId}' was not found in the product list.");
        }
    }
}