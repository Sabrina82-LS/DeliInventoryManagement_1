using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Messaging;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Services.Outbox;

public sealed class OutboxDispatcherV5 : BackgroundService
{
    private readonly CosmosClient _cosmos;
    private readonly CosmosOptions _opt;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxDispatcherV5> _logger;

    private const int MaxAttempts = 5;
    private static readonly TimeSpan LoopDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    // (por enquanto fixo, igual seu projeto)
    private const string StorePkValue = "STORE#1";

    public OutboxDispatcherV5(
        CosmosClient cosmos,
        IOptions<CosmosOptions> opt,
        RabbitMqPublisher publisher,
        ILogger<OutboxDispatcherV5> logger)
    {
        _cosmos = cosmos;
        _opt = opt.Value;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Outbox Dispatcher started (db={DbId}, ops={OpsContainer})",
            _opt.DatabaseId, _opt.Containers.Operations);

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
                _logger.LogError(ex, "❌ Outbox dispatcher loop error");
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
        if (string.IsNullOrWhiteSpace(_opt.DatabaseId))
            throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

        var opsName = _opt.Containers.Operations;
        if (string.IsNullOrWhiteSpace(opsName))
            throw new InvalidOperationException("CosmosDb:Containers:Operations não configurado.");

        var ops = _cosmos.GetContainer(_opt.DatabaseId, opsName);

        var pk = new PartitionKey(StorePkValue);
        var nowUtc = DateTime.UtcNow;

        // Busca só eventos livres (sem lock ou lock expirado) e Pending
        var query = new QueryDefinition(@"
            SELECT * FROM c
            WHERE c.pk = @pk
              AND c.type = 'OutboxEvent'
              AND c.status = 'Pending'
              AND (NOT IS_DEFINED(c.lockedUntilUtc) OR c.lockedUntilUtc = null OR c.lockedUntilUtc < @nowUtc)
            ORDER BY c.updatedAtUtc ASC
        ")
        .WithParameter("@pk", StorePkValue)
        .WithParameter("@nowUtc", nowUtc);

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
        {
            // (log de debug opcional)
            // _logger.LogDebug("📭 Outbox: no pending items found");
            return;
        }

        _logger.LogInformation("📦 Outbox fetched: {Count} pending item(s)", page.Count);

        foreach (var evt in page)
            await ProcessEventAsync(evt, ops, pk, ct);
    }

    private static string ResolveRoutingKey(OutboxEventV5 evt)
    {
        // Preferir EventType, mas fallback por AggregateType
        return evt.EventType switch
        {
            "SaleCreated" => "sale.created",
            "RestockCreated" => "restock.created",
            _ => evt.AggregateType switch
            {
                "SALE" => "sale.created",
                "RESTOCK" => "restock.created",
                _ => throw new InvalidOperationException($"Unknown event type: {evt.EventType} / {evt.AggregateType}")
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
