using Moq;
using RabbitMQ.Client;
using System.Text;
using Xunit;

namespace DelinventoryManagement_1.Api.Tests.Mocks
{
    public class MockRabbitServerTests
    {
        [Fact]
        public void MockRabbitServer_ShouldCapturePublishedMessages()
        {
            // ===== ARRANGE =====
            var server = new MockRabbitServer();
            var testMessage = "Hello RabbitMQ!";
            var testBytes = Encoding.UTF8.GetBytes(testMessage);

            var props = new Mock<IBasicProperties>();
            props.SetupProperty(p => p.MessageId, "test-123");

            // ===== ACT =====
            // Note: In 6.x, BasicPublish is synchronous, not async
            server.Channel.Object.BasicPublish(
                exchange: "deli.exchange",
                routingKey: "inventory.update",
                mandatory: false,
                basicProperties: props.Object,
                body: testBytes);

            // ===== ASSERT =====
            Assert.Single(server.PublishedMessages);
            var published = server.PublishedMessages[0];
            Assert.Equal("deli.exchange", published.Exchange);
            Assert.Equal("inventory.update", published.RoutingKey);
            Assert.Equal(testMessage, published.Message);
            Assert.Equal("test-123", published.MessageId);
        }

        [Fact]
        public void MockRabbitServer_ShouldCaptureMultipleMessages()
        {
            // ===== ARRANGE =====
            var server = new MockRabbitServer();

            // ===== ACT =====
            server.Channel.Object.BasicPublish(
                exchange: "deli.exchange",
                routingKey: "inventory.update",
                mandatory: false,
                basicProperties: null,
                body: Encoding.UTF8.GetBytes("First message"));

            server.Channel.Object.BasicPublish(
                exchange: "deli.exchange",
                routingKey: "stock.alert",
                mandatory: false,
                basicProperties: null,
                body: Encoding.UTF8.GetBytes("Second message"));

            // ===== ASSERT =====
            Assert.Equal(2, server.PublishedMessages.Count);
            Assert.Equal("First message", server.PublishedMessages[0].Message);
            Assert.Equal("Second message", server.PublishedMessages[1].Message);
        }

        [Fact]
        public void MockRabbitServer_ShouldWorkWithoutMessageId()
        {
            // ===== ARRANGE =====
            var server = new MockRabbitServer();

            // ===== ACT =====
            server.Channel.Object.BasicPublish(
                exchange: "test.exchange",
                routingKey: "test.key",
                mandatory: false,
                basicProperties: null,
                body: Encoding.UTF8.GetBytes("No ID message"));

            // ===== ASSERT =====
            Assert.Single(server.PublishedMessages);
            Assert.Null(server.PublishedMessages[0].MessageId);
            Assert.Equal("No ID message", server.PublishedMessages[0].Message);
        }
    }
}