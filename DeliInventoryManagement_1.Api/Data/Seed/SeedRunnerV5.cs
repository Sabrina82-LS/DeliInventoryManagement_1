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
        // Only run seed in Development
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

            var storePk = options.DefaultStorePk;

            // Seed suppliers first
            await SeedSuppliersAsync(suppliersContainer, storePk);

            // Then seed products
            await SeedProductsAsync(productsContainer, storePk);

            Console.WriteLine("✅ SeedRunnerV5 completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ SeedRunnerV5 failed, but the API will continue running.");
            Console.WriteLine(ex);
        }
    }

    private static async Task SeedSuppliersAsync(Container container, string storePk)
    {
        foreach (var supplier in SupplierSeed.GetSuppliers())
        {
            if (supplier is null)
                continue;

            supplier.Pk = storePk;
            supplier.Type = "Supplier";

            EnsureRequiredFields(supplier.Id, supplier.Pk, "Supplier");

            await container.UpsertItemAsync(supplier, new PartitionKey(supplier.Pk));
        }
    }

    private static async Task SeedProductsAsync(Container container, string storePk)
    {
        // Extra safety: remove any remaining products for this store
        await DeleteAllProductsForStoreAsync(container, storePk);

        foreach (var product in ProductSeed.GetProducts())
        {
            if (product is null)
                continue;

            product.Pk = storePk;
            product.Type = "Product";

            EnsureRequiredFields(product.Id, product.Pk, "Product");

            await container.UpsertItemAsync(product, new PartitionKey(product.Pk));
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

    private static async Task DeleteAllProductsForStoreAsync(Container container, string storePk)
    {
        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.pk = @pk")
            .WithParameter("@pk", storePk);

        using var iterator = container.GetItemQueryIterator<ProductIdOnly>(
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
                    await container.DeleteItemAsync<ProductV5>(item.Id, new PartitionKey(storePk));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Item was already deleted, ignore
                }
            }
        }
    }

    private sealed class ProductIdOnly
    {
        public string Id { get; set; } = string.Empty;
    }
}