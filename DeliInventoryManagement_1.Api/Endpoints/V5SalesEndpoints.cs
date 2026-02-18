using System.Net;
using System.Globalization;
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

            var pkValue = CosmosContainerFactory.StorePk;   // "STORE#1"
            var pk = new PartitionKey(pkValue);

            var products = factory.Products();
            var ops = factory.Operations();

            // ✅ Use DateTime (seus ModelsV5 usam DateTime, não string)
            var nowUtc = DateTime.UtcNow;
            var saleDateUtc = (req.Date ?? nowUtc).ToUniversalTime();

            // ============================
            // 1) Normaliza/agrupa linhas
            // ============================
            var grouped = req.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductId) && l.Quantity > 0)
                .GroupBy(l => l.ProductId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (grouped.Count == 0)
                return Results.BadRequest("Invalid sale lines.");

            // ============================
            // 2) Carrega produtos + valida estoque + captura ETag
            // ============================
            var loaded = new Dictionary<string, (ProductV5 Product, string ETag)>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in grouped)
            {
                try
                {
                    var read = await products.ReadItemAsync<ProductV5>(line.ProductId, pk);
                    var p = read.Resource;

                    if (!string.Equals(p.Type, "Product", StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest($"'{line.ProductId}' is not a Product document.");

                    if (p.Quantity < line.Quantity)
                        return Results.BadRequest($"Not enough stock for '{p.Name}'. Available={p.Quantity}, Requested={line.Quantity}");

                    loaded[line.ProductId] = (p, read.ETag);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Results.NotFound($"Product '{line.ProductId}' not found.");
                }
            }

            // ============================
            // 3) TransactionalBatch no PRODUCTS (ATÔMICO) com ETag
            // ============================
            var productsBatch = products.CreateTransactionalBatch(pk);

            foreach (var line in grouped)
            {
                var etag = loaded[line.ProductId].ETag;

                productsBatch.PatchItem(
                    id: line.ProductId,
                    patchOperations: new[]
                    {
                        PatchOperation.Increment("/quantity", -line.Quantity),
                        // ✅ mantém tipo consistente (DateTime)
                        PatchOperation.Set("/updatedAtUtc", nowUtc)
                    },
                    requestOptions: new TransactionalBatchPatchItemRequestOptions
                    {
                        IfMatchEtag = etag
                    });
            }

            var productsResp = await productsBatch.ExecuteAsync();

            if (!productsResp.IsSuccessStatusCode)
            {
                return Results.Problem(
                    title: "Products TransactionalBatch failed",
                    detail: $"Status: {(int)productsResp.StatusCode} {productsResp.StatusCode}",
                    statusCode: (int)productsResp.StatusCode);
            }

            // ============================
            // 4) Cria Sale (Operations)
            // ============================
            var sale = new SaleV5
            {
                Id = $"SALE#{Guid.NewGuid():N}",
                Pk = pkValue,
                Type = "Sale",

                Date = saleDateUtc,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,

                Lines = grouped.Select(g =>
                {
                    var p = loaded[g.ProductId].Product;
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

            // ============================
            // 5) StockMovement (Operations)
            // ============================
            var movement = new StockMovementV5
            {
                Id = $"MOVE#{Guid.NewGuid():N}",
                Pk = pkValue,
                Type = "StockMovement",
                Reason = "SALE",
                RefId = sale.Id,

                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,

                Lines = grouped.Select(g => new StockMovementLineV5
                {
                    ProductId = g.ProductId,
                    Delta = -g.Quantity
                }).ToList()
            };

            // ============================
            // 6) OutboxEvent (Pending) (Operations)
            // ============================
            var outbox = new OutboxEventV5
            {
                Id = $"OUTBOX#{Guid.NewGuid():N}",
                Pk = pkValue,
                Type = "OutboxEvent",

                Status = "Pending",
                EventType = "SaleCreated",
                AggregateType = "SALE",
                AggregateId = sale.Id,

                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,

                Payload = new
                {
                    saleId = sale.Id,
                    date = sale.Date, // DateTime (ok)
                    total = sale.Total,
                    lines = sale.Lines.Select(x => new
                    {
                        productId = x.ProductId,
                        productName = x.ProductName,
                        quantity = x.Quantity,
                        unitPrice = x.UnitPrice
                    }).ToList()
                }
            };

            // ============================
            // 7) TransactionalBatch no OPERATIONS
            // ============================
            var opsBatch = ops.CreateTransactionalBatch(pk)
                .CreateItem(sale)
                .CreateItem(movement)
                .CreateItem(outbox);

            var opsResp = await opsBatch.ExecuteAsync();

            if (!opsResp.IsSuccessStatusCode)
            {
                return Results.Problem(
                    title: "Operations TransactionalBatch failed",
                    detail: $"Status: {(int)opsResp.StatusCode} {opsResp.StatusCode}",
                    statusCode: (int)opsResp.StatusCode);
            }

            return Results.Created($"/api/v5/sales/{sale.Id}", new
            {
                saleId = sale.Id,
                sale.Total,
                movementId = movement.Id,
                outboxId = outbox.Id
            });
        })
        .WithTags("5 - Inventory V5 (Hybrid Cosmos /pk)");
    }

    // OBS: Mantido caso você use em outro lugar, mas não é necessário aqui
    private static string ToIsoZ(DateTime utc)
    {
        var dt = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }
}
