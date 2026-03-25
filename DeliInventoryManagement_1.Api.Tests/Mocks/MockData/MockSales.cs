using System;
using System.Collections.Generic;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.ModelsV5.Line;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockData
{
    public static class MockSales
    {
        public static List<SaleV5> GetMockSales()
        {
            return new List<SaleV5>
            {
                new SaleV5
                {
                    Id = "1",
                    Pk = "STORE#1",
                    Type = "Sale",
                    Date = DateTime.UtcNow.AddDays(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-5),
                    Total = 150.00m,
                    Lines = new List<SaleLineV5>
                    {
                        new SaleLineV5
                        {
                            ProductId = "P001",
                            ProductName = "Product 1",
                            Quantity = 2,
                            UnitPrice = 50.00m
                        },
                        new SaleLineV5
                        {
                            ProductId = "P002",
                            ProductName = "Product 2",
                            Quantity = 1,
                            UnitPrice = 50.00m
                        }
                    }
                },
                new SaleV5
                {
                    Id = "2",
                    Pk = "STORE#1",
                    Type = "Sale",
                    Date = DateTime.UtcNow.AddDays(-3),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-3),
                    Total = 275.50m,
                    Lines = new List<SaleLineV5>
                    {
                        new SaleLineV5
                        {
                            ProductId = "P003",
                            ProductName = "Product 3",
                            Quantity = 3,
                            UnitPrice = 91.83m
                        }
                    }
                },
                new SaleV5
                {
                    Id = "3",
                    Pk = "STORE#1",
                    Type = "Sale",
                    Date = DateTime.UtcNow.AddDays(-1),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    Total = 89.99m,
                    Lines = new List<SaleLineV5>
                    {
                        new SaleLineV5
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

        public static SaleV5 GetNewSale()
        {
            return new SaleV5
            {
                Id = Guid.NewGuid().ToString(),
                Pk = "STORE#1",
                Type = "Sale",
                Date = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Total = 10.99m,
                Lines = new List<SaleLineV5>
                {
                    new SaleLineV5
                    {
                        ProductId = "prod-004",
                        ProductName = "New Product",
                        Quantity = 1,
                        UnitPrice = 10.99m
                    }
                }
            };
        }
    }
}