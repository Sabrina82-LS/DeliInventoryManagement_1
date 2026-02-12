using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5OutboxEndpoints
{
    private const string StorePkValue = "STORE#1";

    public static void MapV5OutboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/outbox")
            .WithTags("Outbox V5");

        // ✅ GET /api/v5/outbox/pending
        group.MapGet("/pending", async (
            CosmosClient cosmos,
            IOptions<CosmosOptions> opt,
            CancellationToken ct) =>
        {
            return Results.Ok(await QueryOutboxAsync(
                cosmos, opt.Value, status: "Pending", ct));
        });

        // ✅ GET /api/v5/outbox/published
        group.MapGet("/published", async (
            CosmosClient cosmos,
            IOptions<CosmosOptions> opt,
            CancellationToken ct) =>
        {
            return Results.Ok(await QueryOutboxAsync(
                cosmos, opt.Value, status: "Published", ct));
        });

        // ✅ GET /api/v5/outbox/failed
        group.MapGet("/failed", async (
            CosmosClient cosmos,
            IOptions<CosmosOptions> opt,
            CancellationToken ct) =>
        {
            return Results.Ok(await QueryOutboxAsync(
                cosmos, opt.Value, status: "Failed", ct));
        });

        // (opcional, mas MUITO útil) ✅ GET /api/v5/outbox/by-event/SaleCreated
        group.MapGet("/by-event/{eventType}", async (
            string eventType,
            CosmosClient cosmos,
            IOptions<CosmosOptions> opt,
            CancellationToken ct) =>
        {
            return Results.Ok(await QueryOutboxByEventTypeAsync(
                cosmos, opt.Value, eventType, ct));
        });

        // (opcional) ✅ GET /api/v5/outbox/by-id/{id}
        group.MapGet("/by-id/{id}", async (
            string id,
            CosmosClient cosmos,
            IOptions<CosmosOptions> opt,
            CancellationToken ct) =>
        {
            var container = GetOperationsContainer(cosmos, opt.Value);

            try
            {
                var resp = await container.ReadItemAsync<OutboxEventV5>(
                    id, new PartitionKey(StorePkValue), cancellationToken: ct);

                return Results.Ok(resp.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound(new { message = "OutboxEvent not found", id });
            }
        });
    }

    // -------------------------
    // Helpers
    // -------------------------
    private static Container GetOperationsContainer(CosmosClient cosmos, CosmosOptions opt)
    {
        if (string.IsNullOrWhiteSpace(opt.DatabaseId))
            throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

        var ops = opt.Containers.Operations;
        if (string.IsNullOrWhiteSpace(ops))
            throw new InvalidOperationException("CosmosDb:Containers:Operations não configurado.");

        return cosmos.GetContainer(opt.DatabaseId, ops);
    }

    private static async Task<List<OutboxEventV5>> QueryOutboxAsync(
        CosmosClient cosmos,
        CosmosOptions opt,
        string status,
        CancellationToken ct)
    {
        var container = GetOperationsContainer(cosmos, opt);

        var query = new QueryDefinition(@"
            SELECT * FROM c
            WHERE c.pk = @pk
              AND (c.type = 'OutboxEvent' OR c.type = 'OutboxEventV5')
              AND c.status = @status
            ORDER BY c.createdAtUtc ASC
        ")
        .WithParameter("@pk", StorePkValue)
        .WithParameter("@status", status);

        var results = new List<OutboxEventV5>();

        using var it = container.GetItemQueryIterator<OutboxEventV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(StorePkValue),
                MaxItemCount = 50
            });

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync(ct);
            results.AddRange(page);
        }

        return results;
    }

    private static async Task<List<OutboxEventV5>> QueryOutboxByEventTypeAsync(
        CosmosClient cosmos,
        CosmosOptions opt,
        string eventType,
        CancellationToken ct)
    {
        var container = GetOperationsContainer(cosmos, opt);

        var query = new QueryDefinition(@"
            SELECT * FROM c
            WHERE c.pk = @pk
              AND (c.type = 'OutboxEvent' OR c.type = 'OutboxEventV5')
              AND c.eventType = @eventType
            ORDER BY c.createdAtUtc DESC
        ")
        .WithParameter("@pk", StorePkValue)
        .WithParameter("@eventType", eventType);

        var results = new List<OutboxEventV5>();

        using var it = container.GetItemQueryIterator<OutboxEventV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(StorePkValue),
                MaxItemCount = 50
            });

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync(ct);
            results.AddRange(page);
        }

        return results;
    }
}
