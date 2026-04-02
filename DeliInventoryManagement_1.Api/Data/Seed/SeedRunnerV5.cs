using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;

namespace DeliInventoryManagement_1.Api.Data.Seed;

public static class SeedRunnerV5
{
    public static async Task RunAsync(IServiceProvider services, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        try
        {
            using var scope = services.CreateScope();

            var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.DatabaseId))
                throw new InvalidOperationException("CosmosDb: DatabaseId is not configured.");

            var database = cosmos.GetDatabase(options.DatabaseId);

            var productsContainer = database.GetContainer(options.Containers.Products);
            var suppliersContainer = database.GetContainer(options.Containers.Suppliers);
            var reorderRulesContainer = database.GetContainer(options.Containers.ReorderRules);

            var storePk = options.DefaultStorePk;

            var suppliers = SupplierSeed.GetSuppliers();
            var products = ProductSeed.GetProducts();
            var reorderRules = ReorderRulesSeed.GetRules(products, suppliers);

            await SeedSuppliersAsync(suppliersContainer, suppliers, storePk);
            await SeedProductsAsync(productsContainer, products, storePk);
            await SeedReorderRulesAsync(reorderRulesContainer, reorderRules, storePk);

            Console.WriteLine("✅ SeedRunnerV5 completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ SeedRunnerV5 failed, but the API will continue running.");
            Console.WriteLine(ex);
        }
    }

    private static async Task SeedSuppliersAsync(Container container, List<SupplierV5> suppliers, string storePk)
    {
        foreach (var supplier in suppliers)
        {
            if (supplier is null)
                continue;

            supplier.Pk = storePk;
            supplier.Type = "Supplier";

            EnsureRequiredFields(supplier.Id, supplier.Pk, "Supplier");

            await container.UpsertItemAsync(supplier, new PartitionKey(supplier.Pk));
        }
    }

    private static async Task SeedProductsAsync(Container container, List<ProductV5> products, string storePk)
    {
        await DeleteItemsByTypeForStoreAsync(container, storePk, "Product");

        foreach (var product in products)
        {
            if (product is null)
                continue;

            product.Pk = storePk;
            product.Type = "Product";

            EnsureRequiredFields(product.Id, product.Pk, "Product");

            await container.UpsertItemAsync(product, new PartitionKey(product.Pk));
        }
    }

    private static async Task SeedReorderRulesAsync(Container container, List<ReorderRuleV5> rules, string storePk)
    {
        await DeleteItemsByTypeForStoreAsync(container, storePk, "ReorderRule");

        foreach (var rule in rules)
        {
            if (rule is null)
                continue;

            rule.Pk = storePk;
            rule.Type = "ReorderRule";
            rule.UpdatedAtUtc = DateTime.UtcNow;

            EnsureRequiredFields(rule.Id, rule.Pk, "ReorderRule");

            await container.UpsertItemAsync(rule, new PartitionKey(rule.Pk));
        }
    }

    private static void EnsureRequiredFields(string? id, string? pk, string entityName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                $"Invalid seed item ({entityName}): 'id' is null or empty.");
        }

        if (string.IsNullOrWhiteSpace(pk))
        {
            throw new InvalidOperationException(
                $"Invalid seed item ({entityName}): 'pk' is null or empty.");
        }
    }

    private static async Task DeleteItemsByTypeForStoreAsync(Container container, string storePk, string type)
    {
        var query = new QueryDefinition(
            "SELECT c.id FROM c WHERE c.pk = @pk AND c.type = @type")
            .WithParameter("@pk", storePk)
            .WithParameter("@type", type);

        using var iterator = container.GetItemQueryIterator<ItemIdOnly>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(storePk)
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();

            foreach (var item in response)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                    continue;

                try
                {
                    await container.DeleteItemAsync<object>(item.Id, new PartitionKey(storePk));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                }
            }
        }
    }

    private sealed class ItemIdOnly
    {
        public string Id { get; set; } = string.Empty;
    }
}