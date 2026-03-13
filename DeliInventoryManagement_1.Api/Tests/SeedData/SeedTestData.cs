using DeliInventoryManagement_1.Api.Models;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Tests.SeedData;

public static class SeedTestData
{
    public static async Task RunAsync(Container inventoryContainer, Container suppliersContainer)
    {
        // ✅ ADICIONADO: Não roda seed se já existir pelo menos 1 Product
        // (evita duplicar 15 produtos a cada execução)
        var existingProducts = await CountByTypeAsync(inventoryContainer, "Product");
        if (existingProducts > 0)
            return;

        // 1) Suppliers
        // ✅ ALTERADO: IDs fixos para evitar duplicação a cada seed
        var suppliers = new List<Supplier>
        {
            new Supplier { Id = "s1", Type = "Supplier", Name = "Local Farm Supplier" },
            new Supplier { Id = "s2", Type = "Supplier", Name = "City Wholesale Supplier" }
        };

        foreach (var s in suppliers)
            await suppliersContainer.UpsertItemAsync(s, new PartitionKey(s.Type));

        // 2) Categories (para o filtro do Blazor funcionar)
        var categories = new List<Category>
        {
            new Category { Id = "c1", Type = "Category", Name = "Meat" },
            new Category { Id = "c2", Type = "Category", Name = "Cheese" },
            new Category { Id = "c3", Type = "Category", Name = "Bakery" },
            new Category { Id = "c4", Type = "Category", Name = "Grocery" },
            new Category { Id = "c5", Type = "Category", Name = "Drinks" }
        };

        foreach (var c in categories)
            await inventoryContainer.UpsertItemAsync(c, new PartitionKey(c.Type));

        // 3) Products (15)
        var products = new List<Product>
        {
            NewProduct("Ham", "c1", "Meat", 20, 2.50m, 3.80m, 5),
            NewProduct("Salami", "c1", "Meat", 15, 3.00m, 4.50m, 5),
            NewProduct("Turkey Breast", "c1", "Meat", 12, 2.80m, 4.20m, 5),

            NewProduct("Cheddar Cheese", "c2", "Cheese", 18, 2.20m, 3.90m, 6),
            NewProduct("Mozzarella", "c2", "Cheese", 25, 1.90m, 3.20m, 8),
            NewProduct("Brie Cheese", "c2", "Cheese", 8, 4.50m, 7.50m, 3),

            NewProduct("Baguette", "c3", "Bakery", 30, 0.80m, 1.50m, 10),
            NewProduct("Whole Wheat Bread", "c3", "Bakery", 22, 1.00m, 2.00m, 8),
            NewProduct("Croissant", "c3", "Bakery", 16, 1.20m, 2.50m, 6),

            NewProduct("Olive Oil", "c4", "Grocery", 14, 5.50m, 9.00m, 4),
            NewProduct("Black Olives", "c4", "Grocery", 20, 1.80m, 3.20m, 6),
            NewProduct("Pickles", "c4", "Grocery", 17, 1.50m, 2.80m, 5),

            NewProduct("Fresh Orange Juice", "c5", "Drinks", 12, 2.00m, 3.50m, 4),
            NewProduct("Mineral Water", "c5", "Drinks", 40, 0.50m, 1.20m, 15),
            NewProduct("Sparkling Water", "c5", "Drinks", 28, 0.60m, 1.40m, 10)
        };

        foreach (var p in products)
            await inventoryContainer.UpsertItemAsync(p, new PartitionKey(p.Type));
    }

    // ✅ ADICIONADO: helper para contar documentos por Type
    private static async Task<int> CountByTypeAsync(Container container, string type)
    {
        var q = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.Type = @type")
            .WithParameter("@type", type);

        using var it = container.GetItemQueryIterator<int>(q);
        var total = 0;

        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync();
            total += resp.Resource.FirstOrDefault();
        }

        return total;
    }

    private static Product NewProduct(string name, string categoryId, string categoryName, int qty, decimal cost, decimal price, int reorder)
        => new Product
        {
            Id = Guid.NewGuid().ToString(),
            Type = "Product",
            Name = name,
            CategoryId = categoryId,
            CategoryName = categoryName,
            Quantity = qty,
            Cost = cost,
            Price = price,
            ReorderLevel = reorder
        };
}

