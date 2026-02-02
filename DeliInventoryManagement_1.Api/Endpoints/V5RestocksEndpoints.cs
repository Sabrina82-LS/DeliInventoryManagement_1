using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5RestocksEndpoints
{
    public static void MapV5RestocksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/restocks")
            .WithTags("Restocks V5");

        // POST /api/v5/restocks
        group.MapPost("", async (
            CosmosClient cosmos,
            IConfiguration cfg,
            CreateRestockRequestV5 req) =>
        {
            // ============================
            // 1) Validações básicas
            // ============================
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

            if (req.Lines.Any(l => l.Quantity <= 0))
            {
                return Results.BadRequest(new
                {
                    message = "Line Quantity must be > 0."
                });
            }

            if (req.Lines.Any(l => l.CostPerUnit <= 0))
            {
                return Results.BadRequest(new
                {
                    message = "CostPerUnit must be > 0."
                });
            }

            // ============================
            // 2) Cosmos setup
            // ============================
            var c = cfg.GetSection("CosmosDb");
            var dbId = c["DatabaseId"] ?? c["DatabaseName"];

            var productsContainerId = c["ProductsContainerId"] ?? "Products";
            var operationsContainerId = c["OperationsContainerId"] ?? "Operations";

            var products = cosmos.GetContainer(dbId!, productsContainerId);
            var operations = cosmos.GetContainer(dbId!, operationsContainerId);

            // 🔑 PartitionKey padrão da loja (confirmado pelo seu print)
            const string storePk = "STORE#1";
            var storePartitionKey = new PartitionKey(storePk);

            // ============================
            // 3) Criar documento Restock (Operations)
            // ============================
            var restock = new RestockV5
            {
                Id = $"REST#{Guid.NewGuid():N}",
                Pk = storePk, // 👈 IMPORTANTE
                Type = "Restock",

                Date = req.Date.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(req.Date, DateTimeKind.Utc)
                    : req.Date.ToUniversalTime(),

                SupplierId = req.SupplierId,
                SupplierName = req.SupplierName,
                CreatedAtUtc = DateTime.UtcNow,

                Lines = req.Lines.Select(l => new RestockLineV5
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    CostPerUnit = l.CostPerUnit
                }).ToList()
            };

            // ============================
            // 4) Atualizar estoque (Products)
            // ============================
            foreach (var line in restock.Lines)
            {
                try
                {
                    var read = await products.ReadItemAsync<ProductV5>(
                        line.ProductId,
                        storePartitionKey);

                    var product = read.Resource;

                    product.Quantity += line.Quantity;

                    await products.ReplaceItemAsync(
                        product,
                        product.Id,
                        storePartitionKey);
                }
                catch (CosmosException ex)
                    when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Results.BadRequest(new
                    {
                        message = $"Product not found: {line.ProductId}"
                    });
                }
            }

            // ============================
            // 5) Salvar Restock em Operations
            // ============================
            await operations.CreateItemAsync(
                restock,
                storePartitionKey);

            // ============================
            // 6) Response
            // ============================
            return Results.Created(
                $"/api/v5/restocks/{restock.Id}",
                new
                {
                    restock.Id,
                    restock.Date,
                    restock.SupplierId,
                    restock.SupplierName,
                    restock.TotalCost,
                    lines = restock.Lines.Count
                });
        });
    }
}
