using System;
using System.Collections.Generic;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.ModelsV5;

namespace DeliInventoryManagement_1.Api.Tests.Utilities
{
    /// <summary>
    /// Factory for creating test data consistently across tests
    /// </summary>
    public static class TestDataFactory
    {
        public static Product CreateProduct(
            string id = null,
            string name = "Test Product",
            string categoryId = "c1",
            string categoryName = "Test Category",
            int quantity = 100,
            decimal price = 10.99m,
            decimal cost = 5.99m)
        {
            return new Product
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Product",
                Name = name,
                CategoryId = categoryId,
                CategoryName = categoryName,
                Quantity = quantity,
                Price = price,
                Cost = cost,
                ReorderLevel = 5
            };
        }

        public static ProductV5 CreateProductV5(
            string id = null,
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

        public static Sales CreateSale(
            string id = null,
            List<SaleLine> lines = null)
        {
            if (lines == null)
            {
                lines = new List<SaleLine>
                {
                    new SaleLine
                    {
                        ProductId = "prod-001",
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 10.99m
                    }
                };
            }

            return new Sales
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Sale",
                Date = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                Lines = lines,
                Total = lines.Sum(l => l.Quantity * l.UnitPrice)
            };
        }

        public static Restock CreateRestock(
            string id = null,
            string supplierId = "s1",
            string supplierName = "Test Supplier",
            List<RestockLine> lines = null)
        {
            if (lines == null)
            {
                lines = new List<RestockLine>
                {
                    new RestockLine
                    {
                        ProductId = "prod-001",
                        ProductName = "Test Product",
                        Quantity = 10,
                        CostPerUnit = 5.50m
                    }
                };
            }

            return new Restock
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Restock",
                Date = DateTime.UtcNow,
                SupplierId = supplierId,
                SupplierName = supplierName,
                Lines = lines,
                TotalCost = lines.Sum(l => l.Quantity * l.CostPerUnit),
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        public static Category CreateCategory(string id = null, string name = "Test Category")
        {
            return new Category
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Category",
                Name = name,
                Description = $"Description for {name}"
            };
        }

        public static Supplier CreateSupplier(string id = null, string name = "Test Supplier")
        {
            return new Supplier
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Type = "Supplier",
                Name = name,
                ContactName = "John Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                Address = "123 Test St"
            };
        }
    }
}