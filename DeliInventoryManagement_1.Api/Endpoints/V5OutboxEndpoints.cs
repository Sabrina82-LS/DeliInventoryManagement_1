using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5OutboxEndpoints
{
    public static void MapV5OutboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/outbox")
            .WithTags("Outbox V5");

        group.MapGet("/pending", async (CosmosClient cosmos, IConfiguration cfg) =>
        {
            var c = cfg.GetSection("CosmosDb");
            var dbId = c["DatabaseId"] ?? c["DatabaseName"];
            var operationsContainerId = c["OperationsContainerId"] ?? "Operations";

            var operations = cosmos.GetContainer(dbId!, operationsContainerId);

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = 'OutboxEvent' AND c.status = 'Pending' ORDER BY c.createdAtUtc ASC"
            );

            var iterator = operations.GetItemQueryIterator<OutboxEventV5>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("STORE#1")
                });

            var results = new List<OutboxEventV5>();
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            return Results.Ok(results);
        });
    }
}
