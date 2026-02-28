using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockData;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockServices;
using DeliInventoryManagement_1.Api.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

            // Replace real RabbitMQ with mock
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IRabbitMqService>(_mockRabbitMq);
                });
            });
        }

        //[Fact]
        //public async Task CreateSale_ShouldPublishRabbitMQEvent()
        //{
        //    // Arrange - Create HTTP client
        //    var client = _factory.CreateClient();

        //    // Create a test sale request
        //    var saleRequest = new
        //    {
        //        date = DateTime.UtcNow,
        //        lines = new[]
        //        {
        //            new { productId = "prod-001", quantity = 2 }
        //        }
        //    };

        //    // Act - Call the real endpoint
        //    var response = await client.PostAsJsonAsync("/api/v5/sales", saleRequest);

        //    // Assert - Check HTTP response
        //    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        //    // ✅ THIS IS THE MAGIC OF MOCKS!
        //    // Check that our mock RECORDED the message
        //    Assert.True(_mockRabbitMq.WasMessagePublished("SaleCreated"));
        //    Assert.Equal(1, _mockRabbitMq.CountMessagesByType("SaleCreated"));
        //}

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

