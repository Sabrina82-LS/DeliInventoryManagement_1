using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockData
{
    public static class MockDataFactory
    {
        public static Sale CreateTestSale()
        {
            return new Sale
            {  
                Id = Guid.NewGuid().ToString(),
                ProductId = "prod-001",
                ProductName = "Test Product",
                Quantity = 2,
                UnitPrice = 10.99m,
                Total = 21.98m,
                //Date = DateTime.UtcNow
            };
        }

        public static Restock CreateTestRestock()
        {
            return new Restock
            {
                Id = Guid.NewGuid().ToString(),
                //ProductId = "prod-001",
                Type = "Test Product",
                Quantity = 10,
                SupplierId = "s1",
                SupplierName = "Test Supplier"
            };
        }

        public static Product CreateTestProduct()
        {
            return new Product
            {
                Id = "prod-001",
                Name = "Test Product",
                CategoryId = "c1",
                CategoryName = "Test Category",
                Quantity = 100,
                Price = 10.99m,
                Cost = 5.99m
            };
        }
    }
}