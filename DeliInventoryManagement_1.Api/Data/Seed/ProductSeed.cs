using DeliInventoryManagement_1.Api.Models;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class ProductSeed
{
    public static List<Product> GetProducts()
    {
        // Produtos fixos por categoria para ficar consistente e previsível no Blazor
        var data = new[]
        {
            new { CatId = "c1", CatName = "Meat",       Names = new[] { "Ham", "Salami", "Turkey Breast", "Ground Beef" } },
            new { CatId = "c2", CatName = "Dairy",      Names = new[] { "Milk 1L", "Cheddar Cheese", "Butter", "Natural Yogurt" } },
            new { CatId = "c3", CatName = "Beverages",  Names = new[] { "Orange Juice", "Mineral Water", "Sparkling Water", "Cola Soda" } },
            new { CatId = "c4", CatName = "Bakery",     Names = new[] { "Baguette", "Whole Wheat Bread", "Croissant", "Rolls" } },
            new { CatId = "c5", CatName = "Vegetables", Names = new[] { "Tomatoes", "Potatoes", "Onions", "Carrots" } },
        };

        var products = new List<Product>();

        foreach (var group in data)
        {
            for (int i = 0; i < group.Names.Length; i++)
            {
                // ID: CategoryId + "-" + 2 dígitos (01..99)
                var id = $"{group.CatId}-{(i + 1):00}";

                // custos simples e consistentes
                var cost = 1.00m + (i * 0.75m);
                var price = Math.Round(cost * 1.6m, 2);

                products.Add(new Product
                {
                    Id = id,
                    Type = "Product",
                    Name = group.Names[i],
                    CategoryId = group.CatId,
                    CategoryName = group.CatName,
                    Quantity = 10 + (i * 5),
                    Cost = Math.Round(cost, 2),
                    Price = price,
                    ReorderLevel = 5
                });
            }
        }

        return products;
    }
}
