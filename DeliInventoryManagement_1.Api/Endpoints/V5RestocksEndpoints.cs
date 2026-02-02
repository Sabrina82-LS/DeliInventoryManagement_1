using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.Dtos.V5.Events;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5RestocksEndpoints
{
    public static void MapV5RestocksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/restocks")
            .WithTags("Restocks V5");

        group.MapPost("", async (
            CosmosClient cosmos,
            IConfiguration cfg,
            CreateRestockRequestV5 req) =>
        {
            // ============================
            // 1) Validações
            // ============================
            if (string.IsNullOrWhiteSpace(req.SupplierId) || string.IsNullOrWhiteSpace(req.SupplierName))
                return Results.BadRequest(new { message = "SupplierId and SupplierName are required." });

            if (req.Lines is null || req.Lines.Count == 0)
                return Results.BadRequest(new { message = "At least one line is required." });

            if (req.Lines.Any(l => string.IsNullOrWhiteSpace(l.ProductId)))
                return Results.BadRequest(new { message = "Each line must have ProductId." });

            if (req.Lines.Any(l => l.Quantity <= 0))
                return Results.BadRequest(new { message = "Line Quantity must be > 0." });

            if (req.Lines.Any(l => l.CostPerUnit <= 0))
                return Results.BadRequest(new { message = "CostPerUnit must be > 0." });

            // ============================
            // 2) Cosmos setup
            // ============================
            var c = cfg.GetSection("CosmosDb");
            var dbId = c["DatabaseId"] ?? c["DatabaseName"];

            var productsContainerId = c["ProductsContainerId"] ?? "Products";
            var operationsContainerId = c["OperationsContainerId"] ?? "Operations";

            var products = cosmos.GetContainer(dbId!, productsContainerId);
            var operations = cosmos.GetContainer(dbId!, operationsContainerId);

            const string storePk = "STORE#1";
            var pk = new PartitionKey(storePk);

            // ============================
            // 3) Normalizar linhas (somar qty do mesmo ProductId)
            // ============================
            var mergedLines = req.Lines
                .GroupBy(l => l.ProductId.Trim())
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName?.Trim() ?? "",
                    Quantity = g.Sum(x => x.Quantity),
                    CostPerUnit = g.Last().CostPerUnit
                })
                .ToList();

            // ============================
            // 4) Criar Restock (Operations)
            // ============================
            var restock = new RestockV5
            {
                Id = $"REST#{Guid.NewGuid():N}",
                Pk = storePk,
                Type = "Restock",

                Date = req.Date.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(req.Date, DateTimeKind.Utc)
                    : req.Date.ToUniversalTime(),

                SupplierId = req.SupplierId.Trim(),
                SupplierName = req.SupplierName.Trim(),
                CreatedAtUtc = DateTime.UtcNow,

                Lines = mergedLines.Select(l => new RestockLineV5
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    CostPerUnit = l.CostPerUnit
                }).ToList()
            };

            // ============================
            // 5) TransactionalBatch no PRODUCTS (atômico)
            // ============================
            var productsBatch = products.CreateTransactionalBatch(pk);

            foreach (var line in restock.Lines)
            {
                productsBatch.PatchItem(
                    id: line.ProductId,
                    patchOperations: new[]
                    {
                        PatchOperation.Increment("/quantity", line.Quantity)
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
            // 6) Criar StockMovement (1 doc com linhas) + Outbox
            // ============================
            var movement = new StockMovementV5
            {
                Id = $"MOVE#{Guid.NewGuid():N}",
                Pk = storePk,
                Type = "StockMovement",
                CreatedAtUtc = DateTime.UtcNow,
                Reason = "RESTOCK",
                RefId = restock.Id,
                Lines = restock.Lines.Select(l => new StockMovementLineV5
                {
                    ProductId = l.ProductId,
                    Delta = l.Quantity // positivo em restock
                }).ToList()
            };

            var evtPayload = new RestockCreatedEventV5
            {
                RestockId = restock.Id,
                SupplierId = restock.SupplierId,
                SupplierName = restock.SupplierName,
                Date = restock.Date,
                TotalCost = restock.TotalCost,
                Lines = restock.Lines.Select(l => new RestockCreatedLineEventV5
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity
                }).ToList()
            };

            var outbox = new OutboxEventV5
            {
                Id = $"OUTBOX#{Guid.NewGuid():N}",
                Pk = storePk,
                Type = "OutboxEvent",

                EventType = "RestockCreated",
                AggregateType = "RESTOCK",
                AggregateId = restock.Id,

                Payload = evtPayload,
                Status = "Pending",
                Attempts = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            // ============================
            // 7) TransactionalBatch no OPERATIONS (restock + movement + outbox juntos)
            // ============================
            var opsBatch = operations.CreateTransactionalBatch(pk)
                .CreateItem(restock)
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

            // ============================
            // 8) Response
            // ============================
            return Results.Created($"/api/v5/restocks/{restock.Id}", new
            {
                restock.Id,
                restock.Date,
                restock.SupplierId,
                restock.SupplierName,
                restock.TotalCost,
                lines = restock.Lines.Count,
                movementId = movement.Id,
                outboxId = outbox.Id
            });
        });
    }
}
