using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Services.Outbox;

public sealed class OutboxDispatcherV5 : BackgroundService
{
    private readonly CosmosClient _cosmos;
    private readonly IConfiguration _cfg;
    private readonly ILogger<OutboxDispatcherV5> _logger;

    private const int MaxAttempts = 5;

    public OutboxDispatcherV5(
        CosmosClient cosmos,
        IConfiguration cfg,
        ILogger<OutboxDispatcherV5> logger)
    {
        _cosmos = cosmos;
        _cfg = cfg;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Outbox Dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Outbox dispatcher error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchOnce(CancellationToken ct)
    {
        var c = _cfg.GetSection("CosmosDb");
        var dbId = c["DatabaseId"] ?? c["DatabaseName"];
        var ops = _cosmos.GetContainer(dbId!, "Operations");

        var pk = new PartitionKey("STORE#1");

        var query = new QueryDefinition(@"
            SELECT * FROM c
            WHERE c.pk = @pk
              AND c.type = 'OutboxEvent'
              AND c.status = 'Pending'
              AND (NOT IS_DEFINED(c.lockedUntilUtc) OR c.lockedUntilUtc < @now)
        ")
        .WithParameter("@pk", "STORE#1")
        .WithParameter("@now", DateTime.UtcNow);

        using var it = ops.GetItemQueryIterator<OutboxEventV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = pk,
                MaxItemCount = 5
            });

        if (!it.HasMoreResults)
            return;

        var page = await it.ReadNextAsync(ct);

        foreach (var evt in page)
        {
            await ProcessEvent(evt, ops, pk, ct);
        }
    }

    private async Task ProcessEvent(
        OutboxEventV5 evt,
        Container ops,
        PartitionKey pk,
        CancellationToken ct)
    {
        try
        {
            // 🔒 lock
            evt.Status = "Processing";
            evt.Attempts++;
            evt.UpdatedAtUtc = DateTime.UtcNow;
            evt.LockedUntilUtc = DateTime.UtcNow.AddSeconds(30);

            await ops.ReplaceItemAsync(evt, evt.Id, pk, cancellationToken: ct);

            // 🔔 SIMULA publicação (RabbitMQ entra no Passo 11)
            _logger.LogInformation(
                "📤 Publishing event {EventType} ({AggregateId})",
                evt.EventType,
                evt.AggregateId);

            // ✅ sucesso
            evt.Status = "Published";
            evt.PublishedAtUtc = DateTime.UtcNow;
            evt.UpdatedAtUtc = DateTime.UtcNow;
            evt.LockedUntilUtc = null;

            await ops.ReplaceItemAsync(evt, evt.Id, pk, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed processing outbox {Id}", evt.Id);

            evt.Status = evt.Attempts >= MaxAttempts ? "Failed" : "Pending";
            evt.LastError = ex.Message;
            evt.UpdatedAtUtc = DateTime.UtcNow;
            evt.LockedUntilUtc = null;

            await ops.ReplaceItemAsync(evt, evt.Id, pk, cancellationToken: ct);
        }
    }
}
