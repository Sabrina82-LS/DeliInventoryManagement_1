using System.Net;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5SalesEndpoints
{
    public static void MapV5Sales(this RouteGroupBuilder v5)
    {
        // GET /api/v5/sales
        v5.MapGet("/sales", async (CosmosContainerFactory factory) =>
        {
            var ops = factory.Operations();
            var pk = CosmosContainerFactory.StorePk;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'Sale' ORDER BY c.createdAtUtc DESC"
            ).WithParameter("@pk", pk);

            var it = ops.GetItemQueryIterator<SaleV5>(query);

            var results = new List<SaleV5>();
            while (it.HasMoreResults)
            {
                var page = await it.ReadNextAsync();
                results.AddRange(page);
            }

            return Results.Ok(results);
        })
        .WithTags("5 - Inventory V5 (Hybrid Cosmos /pk)");

        // POST /api/v5/sales
        v5.MapPost("/sales", async (CreateSaleV5Request req, CosmosContainerFactory factory) =>
        {
            if (req?.Lines == null || req.Lines.Count == 0)
                return Results.BadRequest("Sale must contain at least one line.");

            var pk = CosmosContainerFactory.StorePk;
            var products = factory.Products();
            var ops = factory.Operations();

            // agrupa duplicados
            var grouped = req.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductId) && l.Quantity > 0)
                .GroupBy(l => l.ProductId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (grouped.Count == 0)
                return Results.BadRequest("Invalid sale lines.");

            // carrega produtos e valida estoque
            var loaded = new Dictionary<string, ProductV5>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in grouped)
            {
                try
                {
                    var read = await products.ReadItemAsync<ProductV5>(line.ProductId, new PartitionKey(pk));
                    var p = read.Resource;

                    if (p.Type != "Product")
                        return Results.BadRequest($"'{line.ProductId}' is not a Product document.");

                    if (p.Quantity < line.Quantity)
                        return Results.BadRequest($"Not enough stock for '{p.Name}'. Available={p.Quantity}, Requested={line.Quantity}");

                    loaded[line.ProductId] = p;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Results.NotFound($"Product '{line.ProductId}' not found.");
                }
            }

            // baixa estoque (simples agora; depois evolui p/ TransactionalBatch)
            foreach (var line in grouped)
            {
                await products.PatchItemAsync<dynamic>(
                    id: line.ProductId,
                    partitionKey: new PartitionKey(pk),
                    patchOperations: new[]
                    {
                        PatchOperation.Increment("/quantity", -line.Quantity),
                        PatchOperation.Set("/updatedAtUtc", DateTime.UtcNow)
                    });
            }

            // cria Sale
            var sale = new SaleV5
            {
                Id = $"SALE#{Guid.NewGuid():N}",
                Pk = pk,
                Type = "Sale",
                Date = req.Date ?? DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Lines = grouped.Select(g =>
                {
                    var p = loaded[g.ProductId];
                    return new SaleLineV5
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        Quantity = g.Quantity,
                        UnitPrice = p.Price
                    };
                }).ToList()
            };
            sale.Total = sale.Lines.Sum(x => x.UnitPrice * x.Quantity);

            await ops.CreateItemAsync(sale, new PartitionKey(pk));

            // cria StockMovement
            var movement = new StockMovementV5
            {
                Id = $"MOVE#{Guid.NewGuid():N}",
                Pk = pk,
                Type = "StockMovement",
                Reason = "SALE",
                RefId = sale.Id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Lines = grouped.Select(g => new StockMovementLineV5
                {
                    ProductId = g.ProductId,
                    Delta = -g.Quantity
                }).ToList()
            };
            await ops.CreateItemAsync(movement, new PartitionKey(pk));

            // cria OutboxEvent (para RabbitMQ depois)
            var outbox = new OutboxEventV5
            {
                Id = $"OUTBOX#{Guid.NewGuid():N}",
                Pk = pk,
                Type = "OutboxEvent",
                Status = "Pending",
                EventType = "SaleCreated",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Payload = new
                {
                    saleId = sale.Id,
                    total = sale.Total,
                    lines = sale.Lines.Select(x => new { x.ProductId, x.Quantity, x.UnitPrice }).ToList()
                }
            };
            await ops.CreateItemAsync(outbox, new PartitionKey(pk));

            return Results.Created($"/api/v5/sales/{sale.Id}", new { saleId = sale.Id, sale.Total });
        })
        .WithTags("5 - Inventory V5 (Hybrid Cosmos /pk)");
    }
}
