using DeliInventoryManagement_1.Api.Messaging;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Services.Outbox;

public sealed class OutboxDispatcherV5 : BackgroundService
{
    private readonly CosmosClient _cosmos;
    private readonly IConfiguration _cfg;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxDispatcherV5> _logger;

    private const int MaxAttempts = 5;
    private static readonly TimeSpan LoopDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    public OutboxDispatcherV5(
        CosmosClient cosmos,
        IConfiguration cfg,
        RabbitMqPublisher publisher,
        ILogger<OutboxDispatcherV5> logger)
    {
        _cosmos = cosmos;
        _cfg = cfg;
        _publisher = publisher;
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Outbox dispatcher error");
            }

            try
            {
                await Task.Delay(LoopDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
        }

        _logger.LogInformation("🛑 Outbox Dispatcher stopped");
    }

    private async Task DispatchOnce(CancellationToken ct)
    {
        var dbId = GetDatabaseId();
        var opsContainerName = GetOperationsContainerName();

        var ops = _cosmos.GetContainer(dbId, opsContainerName);

        // (por enquanto fixo, igual seu projeto)
        var storePkValue = "STORE#1";
        var pk = new PartitionKey(storePkValue);

        // ✅ IMPORTANTÍSSIMO:
        // Cosmos armazena DateTime como string ISO (ex: "2026-02-07T15:52:59.59Z")
        // então comparamos string ISO com string ISO.
        var nowIso = DateTime.UtcNow.ToString("O");

        var query = new QueryDefinition(@"
            SELECT * FROM c
            WHERE c.pk = @pk
              AND c.type = 'OutboxEvent'
              AND c.status = 'Pending'
              AND (NOT IS_DEFINED(c.lockedUntilUtc) OR c.lockedUntilUtc < @nowIso)
            ORDER BY c.updatedAtUtc ASC
        ")
        .WithParameter("@pk", storePkValue)
        .WithParameter("@nowIso", nowIso);

        using var it = ops.GetItemQueryIterator<OutboxEventV5>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = pk,
                MaxItemCount = 10
            });

        if (!it.HasMoreResults)
            return;

        var page = await it.ReadNextAsync(ct);

        if (page.Count == 0)
            return;

        _logger.LogInformation("📦 Outbox batch fetched: {Count} item(s)", page.Count);

        foreach (var evt in page)
        {
            await ProcessEventAsync(evt, ops, pk, ct);
        }
    }

    private string GetDatabaseId()
    {
        var c = _cfg.GetSection("CosmosDb");
        var dbId = c["DatabaseId"] ?? c["DatabaseName"];

        if (string.IsNullOrWhiteSpace(dbId))
            throw new InvalidOperationException("CosmosDb: DatabaseId (ou DatabaseName) não configurado.");

        return dbId;
    }

    private string GetOperationsContainerName()
    {
        // tenta pegar do seu appsettings:
        // CosmosDb:Containers:Operations
        var opsName = _cfg["CosmosDb:Containers:Operations"];
        return string.IsNullOrWhiteSpace(opsName) ? "Operations" : opsName;
    }

    private static string ResolveRoutingKey(OutboxEventV5 evt)
    {
        return evt.EventType switch
        {
            "SaleCreated" => "sale.created",
            "RestockCreated" => "restock.created",
            _ => evt.AggregateType switch
            {
                "SALE" => "sale.created",
                "RESTOCK" => "restock.created",
                _ => throw new InvalidOperationException(
                    $"Unknown event type: {evt.EventType} / {evt.AggregateType}")
            }
        };
    }

    private async Task ProcessEventAsync(
        OutboxEventV5 evt,
        Container ops,
        PartitionKey pk,
        CancellationToken ct)
    {
        try
        {
            // 1) Lock + mark Processing
            evt.Status = "Processing";
            evt.Attempts++;
            evt.UpdatedAtUtc = DateTime.UtcNow;
            evt.LockedUntilUtc = DateTime.UtcNow.Add(LockDuration);

            await ops.ReplaceItemAsync(evt, evt.Id, pk, cancellationToken: ct);

            // 2) Publish to RabbitMQ
            var routingKey = ResolveRoutingKey(evt);

            _logger.LogInformation(
                "📤 Publishing {EventType} ({AggregateId}) -> {RoutingKey} (Attempt {Attempt})",
                evt.EventType, evt.AggregateId, routingKey, evt.Attempts);

            _publisher.Publish(routingKey, evt.Payload, evt.Id);

            // 3) Mark Published
            evt.Status = "Published";
            evt.PublishedAtUtc = DateTime.UtcNow;
            evt.UpdatedAtUtc = DateTime.UtcNow;
            evt.LockedUntilUtc = null;
            evt.LastError = null;

            await ops.ReplaceItemAsync(evt, evt.Id, pk, cancellationToken: ct);

            _logger.LogInformation("✅ Published {EventType} ({AggregateId})", evt.EventType, evt.AggregateId);
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
