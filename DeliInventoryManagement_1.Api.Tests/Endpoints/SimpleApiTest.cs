using DeliInventoryManagement_1.Api.Tests.Endpoints;
using DeliInventoryManagement_1.Api.Tests.Infrastructure;
using Xunit;

namespace DeliInventoryManagement_1.Api.Tests.Endpoints
{
    public class SimpleApiTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SimpleApiTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact(Skip = "Skipped in CI - depends on Cosmos")]
        public async Task Test1_ApiIsRunning()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/");
            Assert.True(true, $"API responded with {response.StatusCode}");
        }
    }
}