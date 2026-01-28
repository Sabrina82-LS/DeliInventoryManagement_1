using Microsoft.Azure.Cosmos;
using System.Net;

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
                if (c is null)
                    continue;

                EnsureRequiredFields(c.Type, c.Id, "Category");
                await container.UpsertItemAsync(c, new PartitionKey(c.Type));
            }

            // =====================================================
            // 2) Suppliers: garante que TODOS do seed existam
            // =====================================================
            foreach (var s in SupplierSeed.GetSuppliers())
            {
                if (s is null)
                    continue;

                EnsureRequiredFields(s.Type, s.Id, "Supplier");
                await container.UpsertItemAsync(s, new PartitionKey(s.Type));
            }

            // =====================================================
            // 3) Products: recria/atualiza sempre
            // =====================================================
            foreach (var p in ProductSeed.GetProducts())
            {
                if (p is null)
                    continue;

                EnsureRequiredFields(p.Type, p.Id, "Product");
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

    /// <summary>
    /// Garante campos mínimos para Cosmos:
    /// - Type (PartitionKey /Type)
    /// - Id   (Cosmos exige id)
    /// </summary>
    private static void EnsureRequiredFields(string? type, string? id, string entityName)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException(
                $"Seed item inválido ({entityName}): propriedade 'Type' está vazia/nula (id='{id ?? "?"}'). " +
                "Como o PartitionKey do container é '/Type', todo item precisa ter Type preenchido.");

        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException(
                $"Seed item inválido ({entityName}): propriedade 'Id' está vazia/nula. " +
                "Cosmos exige 'id' para criar/atualizar documentos.");
    }

    // =====================================================
    // Remove produtos com prefixo específico (auto-, p, etc)
    // =====================================================
    private static async Task DeleteProductsByPrefixAsync(Container container, string prefix)
    {
        var q = new QueryDefinition(
                "SELECT VALUE c.id FROM c WHERE c.Type = @type AND STARTSWITH(c.id, @prefix)")
            .WithParameter("@type", "Product")
            .WithParameter("@prefix", prefix);

        // Otimização: query já restringe Type=Product, então pode setar PartitionKey
        using var it = container.GetItemQueryIterator<string>(
            q,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("Product")
            });

        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync();

            foreach (var id in resp)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                try
                {
                    await container.DeleteItemAsync<dynamic>(id, new PartitionKey("Product"));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // já foi apagado, ignora
                }
            }
        }
    }
}
