using Xunit;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockData;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockServices
{
    public class MockRabbitMqServiceTests
    {
        [Fact]
        public void PublishSaleCreated_AddsMessage()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            var sale = MockSales.GetNewSale();

            // Act
            mock.PublishSaleCreatedAsync(sale).Wait();

            // Assert
            Assert.True(mock.WasMessagePublished($"SaleCreated:{sale.Id}"));
            Assert.Equal(1, mock.CountMessagesByType("SaleCreated"));
        }

        [Fact]
        public void PublishRestockCreated_AddsMessage()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            var restock = new Restock { Id = "test-123", Quantity = 10 };

            // Act
            mock.PublishRestockCreatedAsync(restock).Wait();

            // Assert
            Assert.Equal(1, mock.PublishedMessages.Count);
        }

        [Fact]
        public void ClearMessages_RemovesAllMessages()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            var sale = MockSales.GetNewSale();
            mock.PublishSaleCreatedAsync(sale).Wait();
            Assert.Equal(1, mock.PublishedMessages.Count);

            // Act
            mock.ClearMessages();

            // Assert
            Assert.Empty(mock.PublishedMessages);
        }
    }
}