using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class SeedRunner
{
    public static async Task RunAsync(Container container, IWebHostEnvironment env)
    {
        // Só roda seed em Development
        if (!env.IsDevelopment())
            return;

        try
        {
            // =====================================================
            // 0) Limpeza segura: remove apenas produtos antigos
            // =====================================================
            // - auto-* (seed manual antigo)
            // - p*     (seed antigo p1..p50)
            await DeleteProductsByPrefixAsync(container, "auto-");
            await DeleteProductsByPrefixAsync(container, "p");

            // =====================================================
            // 1) Categories: garante que TODAS do seed existam
            // =====================================================
            foreach (var c in CategorySeed.GetCategories())
            {
                EnsureType(c?.Type, c?.Id);
                await container.UpsertItemAsync(c, new PartitionKey(c.Type));
            }

            // =====================================================
            // 2) Suppliers: garante que TODOS do seed existam
            // =====================================================
            foreach (var s in SupplierSeed.GetSuppliers())
            {
                EnsureType(s?.Type, s?.Id);
                await container.UpsertItemAsync(s, new PartitionKey(s.Type));
            }

            // =====================================================
            // 3) Products: recria/atualiza sempre
            // =====================================================
            foreach (var p in ProductSeed.GetProducts())
            {
                EnsureType(p?.Type, p?.Id);
                await container.UpsertItemAsync(p, new PartitionKey(p.Type));
            }

            Console.WriteLine("✅ Seed concluído com sucesso.");
        }
        catch (Exception ex)
        {
            // IMPORTANTE: não derruba a API (Swagger continua abrindo)
            Console.WriteLine("⚠️ Seed falhou, mas a API vai continuar.");
            Console.WriteLine(ex);
        }
    }

    private static void EnsureType(string? type, string? id)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException(
                $"Seed item inválido: propriedade 'Type' está vazia/nula (id='{id ?? "?"}'). " +
                "Como o PartitionKey do container é '/Type', todo item precisa ter Type preenchido.");
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

                try
                {
                    await container.DeleteItemAsync<dynamic>(id, new PartitionKey("Product"));
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // já foi apagado, ignora
                }
            }
        }
    }
}
