using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.ModelsV5.Line;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliInventoryManagement_1.Api.Tests.Utilities
{
    /// <summary>
    /// Factory for creating test data consistently across tests
    /// </summary>
    public static class TestDataFactory
    {
        public static ProductV5 CreateProduct(
            string id = null!,
            string name = "Test Product",
            string categoryId = "c1",
            string categoryName = "Test Category",
            int quantity = 100,
            decimal price = 10.99m,
            decimal cost = 5.99m)
        {
            return new ProductV5
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Pk = "STORE#1",
                Type = "Product",
                Name = name,
                CategoryId = categoryId,
                CategoryName = categoryName,
                Quantity = quantity,
                Price = price,
                Cost = cost,
                ReorderLevel = 5,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        public static ProductV5 CreateProductV5(
            string id = null!,
            string name = "Test Product V5",
            string categoryId = "c1",
            int quantity = 100,
            decimal price = 10.99m)
        {
            return new ProductV5
            {
                Id = id ?? $"PROD-{Guid.NewGuid():N}",
                Pk = "STORE#1",
                Type = "Product",
                Name = name,
                CategoryId = categoryId,
                CategoryName = "Test Category",
                Quantity = quantity,
                Price = price,
                Cost = 5.99m,
                ReorderLevel = 5,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        public static SaleV5 CreateSale(
            string id = null!,
            List<SaleLineV5> lines = null!)
        {
            if (lines == null)
            {
                lines = new List<SaleLineV5>
                {
                    new SaleLineV5
                    {
                        ProductId = "prod-001",
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 10.99m
                    }
                };
            }

            return new SaleV5
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Pk = "STORE#1",
                Type = "Sale",
                Date = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Lines = lines,
                Total = lines.Sum(l => l.Quantity * l.UnitPrice)
            };
        }

        public static RestockV5 CreateRestock(
            string id = null!,
            string supplierId = "s1",
            string supplierName = "Test Supplier",
            List<RestockLineV5> lines = null!)
        {
            if (lines == null)
            {
                lines = new List<RestockLineV5>
                {
                    new RestockLineV5
                    {
                        ProductId = "prod-001",
                        ProductName = "Test Product",
                        Quantity = 10,
                        UnitCost = 5.50m
                    }
                };
            }

            return new RestockV5
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Pk = "STORE#1",
                Type = "Restock",
                Date = DateTime.UtcNow,
                SupplierId = supplierId,
                SupplierName = supplierName,
                Lines = lines,
                Total = lines.Sum(l => l.Quantity * l.UnitCost),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        public static Category CreateCategory(string id = null!, string name = "Test Category")
        {
            return new Category
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Category",
                Name = name,
                Description = $"Description for {name}"
            };
        }

        public static SupplierV5 CreateSupplier(string id = null!, string name = "Test Supplier")
        {
            return new SupplierV5
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Pk = "STORE#1",
                Type = "Supplier",
                Name = name,
                Email = "john@example.com",
                Phone = "123-456-7890",
                Notes = "Test supplier notes",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }
    }
}