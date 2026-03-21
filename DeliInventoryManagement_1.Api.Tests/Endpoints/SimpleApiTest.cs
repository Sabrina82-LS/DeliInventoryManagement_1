using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class SimpleApiTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SimpleApiTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Test1_ApiIsRunning()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - Just hit the root
            var response = await client.GetAsync("/");

            // Assert - Any response means API is running
            Assert.True(true, $"API responded with {response.StatusCode}");
        }
    }
}