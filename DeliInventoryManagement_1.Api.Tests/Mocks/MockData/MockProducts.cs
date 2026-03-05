using System.Collections.Generic;
using DeliInventoryManagement_1.Api.Models;


namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockData
{
    public static class MockProducts
    {
        public static List<Product> GetSampleProducts()
        {
            return new List<Product>
        {
            new Product
            {
                Id = "prod-001",
                Type = "Product",
                Name = "Turkey Sandwich",
                CategoryId = "cat-001",
                CategoryName = "Sandwiches",
                Quantity = 15,
                Cost = 4.50m,
                Price = 8.99m,
                ReorderLevel = 5,
             
            },
            new Product
            {
                Id = "prod-002",
                Type = "Product",
                Name = "Caesar Salad",
                CategoryId = "cat-002",
                CategoryName = "Salads",
                Quantity = 8,
                Cost = 3.75m,
                Price = 7.50m,
                ReorderLevel = 5,
               
            },
            new Product
            {
                Id = "prod-003",
                Type = "Product",
                Name = "Italian BMT",
                CategoryId = "cat-001",
                CategoryName = "Sandwiches",
                Quantity = 12,
                Cost = 5.25m,
                Price = 9.49m,
                ReorderLevel = 5,
           
            }
        };
        }

        public static Product GetLowStockProduct()
        {
            return new Product
            {
                Id = "prod-low-001",
                Type = "Product",
                Name = "Low Stock Item",
                CategoryId = "cat-test-001",
                CategoryName = "Test",
                Quantity = 2,  // Below ReorderLevel (5)
                Cost = 3.00m,
                Price = 5.99m,
                ReorderLevel = 5,
             
            };
        }

        public static Product GetInactiveProduct()
        {
            return new Product
            {
                Id = "prod-inactive-001",
                Type = "Product",
                Name = "Discontinued Item",
                CategoryId = "cat-test-002",
                CategoryName = "Test",
                Quantity = 0,
                Cost = 2.50m,
                Price = 6.99m,
                ReorderLevel = 5,
        
            };
        }

        public static Product GetOutOfStockProduct()
        {
            return new Product
            {
                Id = "prod-oos-001",
                Type = "Product",
                Name = "Out of Stock Item",
                CategoryId = "cat-test-003",
                CategoryName = "Test",
                Quantity = 0,
                Cost = 4.00m,
                Price = 8.99m,
                ReorderLevel = 5,
                
            };
        }
    }
}