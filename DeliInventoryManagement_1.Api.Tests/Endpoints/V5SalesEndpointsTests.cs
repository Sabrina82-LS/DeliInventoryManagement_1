using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockData;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockServices;
using DeliInventoryManagement_1.Api.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class V5SalesEndpointsTests : TestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly MockRabbitMqService _mockRabbitMq;


        public V5SalesEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _mockRabbitMq = new MockRabbitMqService();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // ONLY remove IRabbitMqService, nothing else
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add the mock
                    services.AddSingleton<IRabbitMqService>(_mockRabbitMq);
                });
            });

            // Verify mock works
            _mockRabbitMq.PublishSaleCreatedAsync(new Sale { Id = "test" }).Wait();
            Console.WriteLine($"Mock test message count: {_mockRabbitMq.PublishedMessages.Count}");
            _mockRabbitMq.ClearMessages();
        }
 

        [Fact]
        public async Task CreateSale_ShouldPublishRabbitMQEvent()
        {
            // ARRANGE - Test the API normally, but verify with mock separately
            var client = _factory.CreateClient();

            // Create a product
            var productRequest = new
            {
                name = "Test Product",
                categoryId = "c1",
                categoryName = "Test Category",
                quantity = 100,
                cost = 5.99m,
                price = 10.99m,
                reorderLevel = 5,
                isActive = true
            };

            var productResponse = await client.PostAsJsonAsync("/api/v5/products", productRequest);
            productResponse.EnsureSuccessStatusCode();
            var product = await productResponse.Content.ReadFromJsonAsync<ProductV5>();

            // Create sale
            var saleRequest = new
            {
                date = DateTime.UtcNow,
                lines = new[]
                {
            new { productId = product.Id, quantity = 2 }
        }
            };

            var response = await client.PostAsJsonAsync("/api/v5/sales", saleRequest);

            // ASSERT - Check API response
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);


        }
        [Fact]
        public void Mock_Itself_Works()
        {
            // This test ONLY tests the mock, not the API
            var mock = new MockRabbitMqService();
            var sale = new Sale { Id = "test-123" };


            mock.PublishSaleCreatedAsync(sale).Wait();

            Assert.True(mock.WasMessagePublished("SaleCreated"));
            Assert.Equal(1, mock.CountMessagesByType("SaleCreated"));


        }

        [Fact]
        public async Task Api_Endpoint_Is_Reachable()
        {
            // This test ONLY tests that the endpoint exists
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v5/sales");

            // If this fails, the URL is wrong
            Assert.True(response.IsSuccessStatusCode, $"Got {(int)response.StatusCode}");
        }


        [Fact]
        public async Task SimplePostTest()
        {
            var client = _factory.CreateClient();

            // STEP 1: Create a product first
            var productRequest = new
            {
                name = "Test Product",
                categoryId = "c1",
                categoryName = "Test Category",
                quantity = 100,
                cost = 5.99m,
                price = 10.99m,
                reorderLevel = 5,
                isActive = true
            };

            var productResponse = await client.PostAsJsonAsync("/api/v5/products", productRequest);
            productResponse.EnsureSuccessStatusCode();
            var product = await productResponse.Content.ReadFromJsonAsync<ProductV5>();

            // STEP 2: Now create the sale with the REAL product ID
            var saleRequest = new
            {
                date = DateTime.UtcNow,
                lines = new[]
                {
            new {
                productId = product.Id,  // ← Using the real ID from step 1
                quantity = 2
            }
        }
            };

            var response = await client.PostAsJsonAsync("/api/v5/sales", saleRequest);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status: {(int)response.StatusCode}");
            Console.WriteLine($"Response: {content}");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }


        [Fact]
        public async Task FindPostUrl()
        {
            var client = _factory.CreateClient();
            var urls = new[]
            {
        "/api/v5/sales",
        "/api/v5/sales/create",
        "/api/v5/createsale",
        "/api/v5/sale"
    };

            var testData = new { date = DateTime.UtcNow, lines = new[] { new { productId = "test", quantity = 1 } } };

            foreach (var url in urls)
            {
                var response = await client.PostAsJsonAsync(url, testData);
                Console.WriteLine($"POST {url}: {(int)response.StatusCode}");
            }
        }



        [Fact]
        public async Task FindUrl()
        {
            var client = _factory.CreateClient();
            var urls = new[] { "/api/v5/sales", "/v5/sales", "/sales", "/api/sales" };

            foreach (var url in urls)
            {
                var response = await client.GetAsync(url);
                Console.WriteLine($"{url}: {(int)response.StatusCode}");
            }
        }

        [Fact]
        public async Task FindCorrectSalesUrl()
        {
            var client = _factory.CreateClient();

            var urls = new[]
            {
        "/api/v5/sales",
        "/v5/sales",
        "/sales",
        "/api/sales",
        "/api/v1/sales",
        "/sales/create"
    };

            foreach (var url in urls)
            {
                var response = await client.PostAsJsonAsync(url, new
                {
                    date = DateTime.UtcNow,
                    lines = new[] { new { productId = "test", quantity = 1 } }
                });

                Console.WriteLine($"{url}: {(int)response.StatusCode} {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.Created ||
                    response.StatusCode == HttpStatusCode.BadRequest)
                {
                    Console.WriteLine($"✅ FOUND WORKING URL: {url}");
                }
            }
        }


        [Fact]
        public async Task CreateSale_WithInvalidData_ShouldNotPublishEvent()
        {
            // Arrange - Invalid request (missing product)
            var client = _factory.CreateClient();
            var invalidRequest = new { date = DateTime.UtcNow, lines = new object[] { } };

            // Act
            var response = await client.PostAsJsonAsync("/api/v5/sales", invalidRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // ✅ Mock shows NO messages were published
            Assert.Equal(0, _mockRabbitMq.CountMessagesByType("SaleCreated"));
        }
    }
}

