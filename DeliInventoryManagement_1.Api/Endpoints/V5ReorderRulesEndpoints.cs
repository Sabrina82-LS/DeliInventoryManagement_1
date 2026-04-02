using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class V5ReorderRulesEndpoints
{
    public static void MapV5ReorderRules(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/reorderrules")
            .WithTags("Reorder Rules V5")
            .RequireAuthorization();

        group.MapGet("", async (CosmosClient cosmos, IConfiguration cfg) =>
        {
            var dbId = cfg["CosmosDb:DatabaseId"];
            var containerId = cfg["CosmosDb:Containers:ReorderRules"];
            var storePk = cfg["CosmosDb:DefaultStorePk"] ?? "STORE#1";

            if (string.IsNullOrWhiteSpace(dbId))
                return Results.Problem("CosmosDb:DatabaseId is not configured.");

            if (string.IsNullOrWhiteSpace(containerId))
                return Results.Problem("CosmosDb:Containers:ReorderRules is not configured.");

            var container = cosmos.GetContainer(dbId, containerId);

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'ReorderRule' ORDER BY c.productName")
                .WithParameter("@pk", storePk);

            var iterator = container.GetItemQueryIterator<ReorderRuleV5>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(storePk)
                });

            var results = new List<ReorderRuleV5>();

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            return Results.Ok(results);
        });
    }
}