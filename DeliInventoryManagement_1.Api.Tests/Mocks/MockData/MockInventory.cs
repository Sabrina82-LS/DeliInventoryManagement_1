using DeliInventoryManagement_1.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliInventoryManagement_1.Api.Tests.Mocks.MockData
{
    public class InventoryTransaction
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantityChange { get; set; }  // Positive for inbound, negative for outbound
        public string TransactionType { get; set; }  // "Purchase", "Sale", "Return", "Adjustment"
        public DateTime TransactionDate { get; set; }
        public string Notes { get; set; }
    }

    public static class MockInventory
    {
        private static List<Product> _products = new List<Product>();
        private static List<InventoryTransaction> _transactions = new List<InventoryTransaction>();
        private static int _nextTransactionId = 1;

        static MockInventory()
        {
            // Initialize with sample products
            _products = new List<Product>
        {
            new Product
            {
                Id = "prod-001",
                Name = "Turkey Sandwich",
                CategoryName = "Sandwiches",
                Quantity = 15,
                Price = 8.99m,
                ReorderLevel = 5
            },
            new Product
            {
                Id = "prod-002",
                Name = "Caesar Salad",
                CategoryName = "Salads",
                Quantity = 8,
                Price = 7.50m,
                ReorderLevel = 5
            }
        };

            // Add some sample transactions
            _transactions = new List<InventoryTransaction>
        {
            new InventoryTransaction
            {
                Id = "trans-001",
                ProductId = "prod-001",
                ProductName = "Turkey Sandwich",
                QuantityChange = 20,
                TransactionType = "Purchase",
                TransactionDate = DateTime.Now.AddDays(-5),
                Notes = "Weekly restock"
            },
            new InventoryTransaction
            {
                Id = "trans-002",
                ProductId = "prod-001",
                ProductName = "Turkey Sandwich",
                QuantityChange = -5,
                TransactionType = "Sale",
                TransactionDate = DateTime.Now.AddDays(-2),
                Notes = "Customer orders"
            }
        };
        }

        // Stock operations
        public static void AddStock(string productId, int quantity, string notes = "")
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.Quantity += quantity;

                _transactions.Add(new InventoryTransaction
                {
                    Id = $"trans-{_nextTransactionId++:D3}",
                    ProductId = productId,
                    ProductName = product.Name,
                    QuantityChange = quantity,
                    TransactionType = "Purchase",
                    TransactionDate = DateTime.Now,
                    Notes = notes
                });
            }
        }

        public static bool RemoveStock(string productId, int quantity, string notes = "")
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product != null && product.Quantity >= quantity)
            {
                product.Quantity -= quantity;

                _transactions.Add(new InventoryTransaction
                {
                    Id = $"trans-{_nextTransactionId++:D3}",
                    ProductId = productId,
                    ProductName = product.Name,
                    QuantityChange = -quantity,
                    TransactionType = "Sale",
                    TransactionDate = DateTime.Now,
                    Notes = notes
                });

                return true;
            }
            return false;
        }

        // Reports
        public static List<InventoryTransaction> GetTransactionHistory(string productId = null)
        {
            if (productId == null)
                return _transactions.OrderByDescending(t => t.TransactionDate).ToList();

            return _transactions
                .Where(t => t.ProductId == productId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        public static List<Product> GetProductsNeedingReorder() =>
            _products.Where(p => p.Quantity <= p.ReorderLevel).ToList();

        public static Dictionary<string, object> GetInventoryReport()
        {
            return new Dictionary<string, object>
        {
            { "AsOf", DateTime.Now },
            { "TotalProducts", _products.Count },
            { "TotalValue", _products.Sum(p => p.Cost * p.Quantity) },
            { "LowStockItems", GetProductsNeedingReorder().Count },
            { "Products", _products.Select(p => new
               {
                   p.Id,
                   p.Name,
                   p.Quantity,
                   p.ReorderLevel,
                   NeedsReorder = p.Quantity <= p.ReorderLevel,
                   Value = p.Cost * p.Quantity
               })
            }
        };
        }
    }
}
