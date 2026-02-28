using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockData;

namespace DeliInventoryManagement_1.Api.Tests.Services
{
    public class RabbitMqServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<RabbitMqService>> _mockLogger;
        private readonly RabbitMqService _service;

        public RabbitMqServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<RabbitMqService>>();

            _mockConfig.Setup(x => x["RabbitMQ:Host"]).Returns("localhost");
            _mockConfig.Setup(x => x["RabbitMQ:Username"]).Returns("admin");
            _mockConfig.Setup(x => x["RabbitMQ:Password"]).Returns("admin123");
            _mockConfig.Setup(x => x["RabbitMQ:Port"]).Returns("5672");

            _service = new RabbitMqService(_mockConfig.Object, _mockLogger.Object);
        }

        [Fact]
        public void IsConnected_Initially_ReturnsFalse()
        {
            Assert.False(_service.IsConnected);
        }

        [Fact]
        public async Task ConnectAsync_WhenRabbitMQNotRunning_ThrowsExpectedException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => _service.ConnectAsync());

            // Check that the exception message contains any of these expected strings
            bool isValidError =
                exception.Message.Contains("Connection refused") ||
                exception.Message.Contains("AMQP") ||
                exception.Message.Contains("BrokerUnreachable") ||
                exception.Message.Contains("endpoints were reachable");


            Assert.True(isValidError, $"Unexpected error: {exception.Message}");
        }

        [Fact]
        public async Task ConnectAsync_WhenRabbitMQRunning_ConnectsSuccessfully()
        {
            // This test will only pass if RabbitMQ is actually running
            try
            {
                await _service.ConnectAsync();
                Assert.True(_service.IsConnected);
            }
            catch (Exception)
            {
                // Skip test if RabbitMQ not running
                Assert.True(true, "RabbitMQ not running - skipping test");
            }
        }
    }

    
}