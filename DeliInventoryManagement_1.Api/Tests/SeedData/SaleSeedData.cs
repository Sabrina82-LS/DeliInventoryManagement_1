using Microsoft.Azure.Cosmos;
using DeliInventoryManagement_1.Api.Models;


namespace DeliInventoryManagement_1.Api.Tests.SeedData;

public static class SaleSeedData
{
    public static async Task CreateSampleSalesAsync(Container itemsContainer)
    {
        // Busca alguns produtos existentes
        var query = new QueryDefinition(
            "SELECT TOP 5 * FROM c WHERE c.Type = 'Product'"
        );

        var iterator = itemsContainer.GetItemQueryIterator<dynamic>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey("Product")
            });

        var products = new List<dynamic>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            products.AddRange(page);
            break;
        }

        if (!products.Any())
            return;

        foreach (var p in products)
        {
            var sale = new Sale
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = p.id,
                ProductName = p.Name,
                CategoryId = p.CategoryId,
                Quantity = 1,
                UnitPrice = p.Price,
                Total = p.Price * 1,
                CreatedAtUtc = DateTime.UtcNow
            };

            // PartitionKey = Type
            await itemsContainer.CreateItemAsync(
                sale,
                new PartitionKey(sale.Type)
            );
        }
    }
}
