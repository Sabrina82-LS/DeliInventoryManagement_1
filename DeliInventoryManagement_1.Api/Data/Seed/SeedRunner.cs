using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class SeedRunner
{
    public static async Task RunAsync(Container container, IWebHostEnvironment env)
    {
        // Só roda seed em Development
        if (!env.IsDevelopment())
            return;

        // =====================================================
        // 0) Limpeza segura: remove apenas produtos antigos
        // =====================================================
        // - auto-* (seed manual antigo)
        // - p*     (seed antigo p1..p50)
        await DeleteProductsByPrefixAsync(container, "auto-");
        await DeleteProductsByPrefixAsync(container, "p");

        // =====================================================
        // 1) Categories: garante que TODAS do seed existam
        // (Upsert é seguro porque os IDs são fixos c1..c5..)
        // =====================================================
        foreach (var c in CategorySeed.GetCategories())
            await container.UpsertItemAsync(c, new PartitionKey(c.Type));

        // =====================================================
        // 2) Suppliers: garante que TODOS do seed existam
        // (Upsert é seguro porque os IDs são fixos s1..s6)
        // =====================================================
        foreach (var s in SupplierSeed.GetSuppliers())
            await container.UpsertItemAsync(s, new PartitionKey(s.Type));

        // =====================================================
        // 3) Products: recria/atualiza sempre (IDs fixos cX-YY)
        // =====================================================
        foreach (var p in ProductSeed.GetProducts())
            await container.UpsertItemAsync(p, new PartitionKey(p.Type));
    }

    // Remove produtos com prefixo específico (auto-, p, etc)
    private static async Task DeleteProductsByPrefixAsync(Container container, string prefix)
    {
        var q = new QueryDefinition(
                "SELECT c.id FROM c WHERE c.Type = @type AND STARTSWITH(c.id, @prefix)")
            .WithParameter("@type", "Product")
            .WithParameter("@prefix", prefix);

        using var it = container.GetItemQueryIterator<dynamic>(q);

        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync();
            foreach (var doc in resp)
            {
                string id = doc.id;
                await container.DeleteItemAsync<dynamic>(id, new PartitionKey("Product"));
            }
        }
    }
}
