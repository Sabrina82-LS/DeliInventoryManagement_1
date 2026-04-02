using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.Dtos.V5.Events;
using DeliInventoryManagement_1.Api.ModelsV5;
using DeliInventoryManagement_1.Api.ModelsV5.Line;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5RestocksEndpoints
{
    public static void MapV5RestocksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/restocks")
            .WithTags("Restocks V5")
            .RequireAuthorization();

        group.MapPost("", CreateRestock);
    }

    private static async Task<IResult> CreateRestock(
        CosmosClient cosmos,
        IConfiguration cfg,
        CreateRestockRequestV5 req)
    {
        try
        {
            // ======================================
            // 1) Validations
            // ======================================
            if (string.IsNullOrWhiteSpace(req.SupplierId) ||
                string.IsNullOrWhiteSpace(req.SupplierName))
            {
                return Results.BadRequest(new
                {
                    message = "SupplierId and SupplierName are required."
                });
            }

            if (req.Lines is null || req.Lines.Count == 0)
            {
                return Results.BadRequest(new
                {
                    message = "At least one line is required."
                });
            }

            if (req.Lines.Any(l => string.IsNullOrWhiteSpace(l.ProductId)))
            {
                return Results.BadRequest(new
                {
                    message = "Each line must have ProductId."
                });
            }

            if (req.Lines.Any(l => l.Quantity <= 0))
            {
                return Results.BadRequest(new
                {
                    message = "Line Quantity must be greater than 0."
                });
            }

            if (req.Lines.Any(l => l.CostPerUnit <= 0))
            {
                return Results.BadRequest(new
                {
                    message = "CostPerUnit must be greater than 0."
                });
            }

            // ======================================
            // 2) Cosmos setup
            // ======================================
            var cosmosSection = cfg.GetSection("CosmosDb");
            var dbId = cosmosSection["DatabaseId"] ?? cosmosSection["DatabaseName"];

            if (string.IsNullOrWhiteSpace(dbId))
            {
                return Results.Problem(
                    title: "Configuration error",
                    detail: "CosmosDb:DatabaseId is not configured.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var productsContainerId =
                cosmosSection["Containers:Products"] ??
                cosmosSection["ProductsContainerId"] ??
                "Products";

            var operationsContainerId =
                cosmosSection["Containers:Operations"] ??
                cosmosSection["OperationsContainerId"] ??
                "Operations";

            var storePk =
                cosmosSection["DefaultStorePk"] ??
                "STORE#1";

            var pk = new PartitionKey(storePk);

            var productsContainer = cosmos.GetContainer(dbId, productsContainerId);
            var operationsContainer = cosmos.GetContainer(dbId, operationsContainerId);

            // ======================================
            // 3) Normalize lines
            // Merge duplicate ProductId lines
            // ======================================
            var mergedLines = req.Lines
                .GroupBy(l => l.ProductId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.Last().ProductName?.Trim() ?? string.Empty,
                    Quantity = g.Sum(x => x.Quantity),
                    CostPerUnit = g.Last().CostPerUnit
                })
                .ToList();

            // ======================================
            // 4) Build restock lines
            // ======================================
            var restockLines = mergedLines
                .Select(l => new RestockLineV5
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    UnitCost = l.CostPerUnit,
                    TotalCost = l.Quantity * l.CostPerUnit
                })
                .ToList();

            var totalCost = restockLines.Sum(x => x.TotalCost);

            // ======================================
            // 5) Create restock document
            // ======================================
            var restockDate = req.Date == default
                ? DateTime.UtcNow
                : req.Date.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(req.Date, DateTimeKind.Utc)
                    : req.Date.ToUniversalTime();

            var restock = new RestockV5
            {
                Id = $"REST#{Guid.NewGuid():N}",
                Pk = storePk,
                Type = "Restock",
                Date = restockDate,
                SupplierId = req.SupplierId.Trim(),
                SupplierName = req.SupplierName.Trim(),
                Lines = restockLines,
                Total = totalCost,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // ======================================
            // 6) Update product quantities
            // ======================================
            var utcNow = DateTime.UtcNow;
            var productsBatch = productsContainer.CreateTransactionalBatch(pk);

            foreach (var line in restock.Lines)
            {
                productsBatch.PatchItem(
                    id: line.ProductId,
                    patchOperations: new[]
                    {
                        PatchOperation.Increment("/quantity", line.Quantity),
                        PatchOperation.Set("/updatedAtUtc", utcNow)
                    });
            }

            var productsResponse = await productsBatch.ExecuteAsync();

            if (!productsResponse.IsSuccessStatusCode)
            {
                return Results.Problem(
                    title: "Products TransactionalBatch failed",
                    detail: $"Status: {(int)productsResponse.StatusCode} {productsResponse.StatusCode}",
                    statusCode: (int)productsResponse.StatusCode);
            }

            // ======================================
            // 7) Create stock movement
            // ======================================
            var movement = new StockMovementV5
            {
                Id = $"MOVE#{Guid.NewGuid():N}",
                Pk = storePk,
                Type = "StockMovement",
                Reason = "RESTOCK",
                RefId = restock.Id,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                Lines = restock.Lines.Select(l => new StockMovementLineV5
                {
                    ProductId = l.ProductId,
                    Delta = l.Quantity
                }).ToList()
            };

            // ======================================
            // 8) Create outbox event
            // ======================================
            var eventPayload = new RestockCreatedEventV5
            {
                RestockId = restock.Id,
                SupplierId = restock.SupplierId,
                SupplierName = restock.SupplierName,
                Date = restock.Date,
                TotalCost = totalCost,
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
                Payload = eventPayload,
                Status = "Pending",
                Attempts = 0,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow
            };

            // ======================================
            // 9) Save restock + movement + outbox
            // ======================================
            var operationsBatch = operationsContainer.CreateTransactionalBatch(pk)
                .CreateItem(restock)
                .CreateItem(movement)
                .CreateItem(outbox);

            var operationsResponse = await operationsBatch.ExecuteAsync();

            if (!operationsResponse.IsSuccessStatusCode)
            {
                return Results.Problem(
                    title: "Operations TransactionalBatch failed",
                    detail: $"Status: {(int)operationsResponse.StatusCode} {operationsResponse.StatusCode}",
                    statusCode: (int)operationsResponse.StatusCode);
            }

            // ======================================
            // 10) Response
            // ======================================
            return Results.Created($"/api/v5/restocks/{restock.Id}", new
            {
                restock.Id,
                restock.Date,
                restock.SupplierId,
                restock.SupplierName,
                restock.Total,
                lines = restock.Lines.Count,
                movementId = movement.Id,
                outboxId = outbox.Id
            });
        }
        catch (CosmosException ex)
        {
            return Results.Problem(
                title: "Cosmos DB error",
                detail: ex.Message,
                statusCode: (int)ex.StatusCode);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Unexpected error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}