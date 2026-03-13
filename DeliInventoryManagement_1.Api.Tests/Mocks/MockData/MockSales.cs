using System;
using System.Collections.Generic;
using System.Linq;
using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockData
{
    public static class MockSales
    {
        public static List<Sales> GetMockSales()
        {
            return new List<Sales>
        {
            new Sales
            {
                Id = "1",
                Date = DateTime.Now.AddDays(-5),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                Total = 150.00m,
                Lines = new List<SaleLine>
                {
                    new SaleLine
                    {
                        ProductId = "P001",
                        ProductName = "Product 1",
                        Quantity = 2,
                        UnitPrice = 50.00m
                    },
                    new SaleLine
                    {
                        ProductId = "P002",
                        ProductName = "Product 2",
                        Quantity = 1,
                        UnitPrice = 50.00m
                    }
                }
            },
            new Sales
            {
                Id = "2",
                Date = DateTime.Now.AddDays(-3),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                Total = 275.50m,
                Lines = new List<SaleLine>
                {
                    new SaleLine
                    {
                        ProductId = "P003",
                        ProductName = "Product 3",
                        Quantity = 3,
                        UnitPrice = 91.83m
                    }
                }
            },
            new Sales
            {
                Id = "3",
                Date = DateTime.Now.AddDays(-1),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                Total = 89.99m,
                Lines = new List<SaleLine>
                {
                    new SaleLine
                    {
                        ProductId = "P004",
                        ProductName = "Product 4",
                        Quantity = 1,
                        UnitPrice = 89.99m
                    }
                }
            }
        };

        }


        public static Sale GetNewSale()
        {
            return new Sale
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = "prod-004",
                ProductName = "New Product",
                Quantity = 1,
                UnitPrice = 10.99m,
                Total = 10.99m,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}