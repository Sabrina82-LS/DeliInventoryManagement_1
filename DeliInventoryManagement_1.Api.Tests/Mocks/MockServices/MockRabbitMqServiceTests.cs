using Xunit;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Tests.Mocks.MockData;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockServices
{
    public class MockRabbitMqServiceTests
    {
        [Fact]
        public async Task PublishSaleCreated_AddsMessage()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            //var mockSales = new MockSales();
            var sale = MockSales.GetNewSale();

            // Act
            //mock.PublishSaleCreatedAsync(sale).Wait();
            await mock.PublishSaleCreatedAsync(sale);

            // Assert
            Assert.True(mock.WasMessagePublished($"SaleCreated:{sale.Id}"));
            Assert.Single(mock.PublishedMessages);
        }

        [Fact]
        public async Task PublishRestockCreated_AddsMessage()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            var restock = new Restock { Id = "test-123", Quantity = 10 };

            // Act
          await mock.PublishRestockCreatedAsync(restock);

            // Assert
            Assert.Single(mock.PublishedMessages);
        }

        [Fact]
        public async Task ClearMessages_RemovesAllMessages()
        {
            // Arrange
            var mock = new MockRabbitMqService();
            //var mockSales = new MockSales();
            var sale = MockSales.GetNewSale();
            await mock.PublishSaleCreatedAsync(sale);
            Assert.Single(mock.PublishedMessages);

            // Act
            mock.ClearMessages();

            // Assert
            Assert.Empty(mock.PublishedMessages);
        }
    }
}