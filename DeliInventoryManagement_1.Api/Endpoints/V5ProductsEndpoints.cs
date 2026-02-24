using System.Net;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints;

public static class V5ProductsEndpoints
{
    public static void MapV5Products(this RouteGroupBuilder v5)
    {
        // ✅ Products = AdminOnly (Admin tem acesso total)
        var group = v5.MapGroup("/products")
            .WithTags("5 - Inventory V5 (Hybrid Cosmos /pk)")
            .RequireAuthorization("AdminOnly");

        // GET /api/v5/products
        group.MapGet("", async (CosmosContainerFactory factory) =>
        {
            var container = factory.Products();
            var pk = CosmosContainerFactory.StorePk;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.pk = @pk AND c.type = 'Product' ORDER BY c.name"
            ).WithParameter("@pk", pk);

            var it = container.GetItemQueryIterator<ProductV5>(query);

            var results = new List<ProductV5>();
            while (it.HasMoreResults)
            {
                var page = await it.ReadNextAsync();
                results.AddRange(page);
            }

            return Results.Ok(results);
        });

        // GET /api/v5/products/{id}
        group.MapGet("/{id}", async (string id, CosmosContainerFactory factory) =>
        {
            var container = factory.Products();
            var pk = CosmosContainerFactory.StorePk;

            try
            {
                var read = await container.ReadItemAsync<ProductV5>(id, new PartitionKey(pk));
                if (read.Resource.Type != "Product") return Results.NotFound();
                return Results.Ok(read.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Results.NotFound();
            }
        });

        // POST /api/v5/products
        group.MapPost("", async (CreateProductV5Request req, CosmosContainerFactory factory) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("Name is required.");

            var container = factory.Products();
            var pk = CosmosContainerFactory.StorePk;

            var product = new ProductV5
            {
                Id = $"PROD#{Guid.NewGuid():N}",
                Pk = pk,
                Type = "Product",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,

                Name = req.Name.Trim(),
                CategoryId = req.CategoryId.Trim(),
                CategoryName = req.CategoryName.Trim(),
                Quantity = req.Quantity,
                Cost = req.Cost,
                Price = req.Price,
                ReorderLevel = req.ReorderLevel,
                ReorderQty = req.ReorderQty,
                IsActive = req.IsActive
            };

            await container.CreateItemAsync(product, new PartitionKey(pk));
            return Results.Created($"/api/v5/products/{product.Id}", product);
        });

        // PUT /api/v5/products/{id}
        group.MapPut("/{id}", async (string id, UpdateProductV5Request req, CosmosContainerFactory factory) =>
        {
            var container = factory.Products();
            var pk = CosmosContainerFactory.StorePk;

            ProductV5 existing;
            try
            {
                var read = await container.ReadItemAsync<ProductV5>(id, new PartitionKey(pk));
                existing = read.Resource;
                if (existing.Type != "Product") return Results.NotFound();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Results.NotFound();
            }

            existing.Name = req.Name.Trim();
            existing.CategoryId = req.CategoryId.Trim();
            existing.CategoryName = req.CategoryName.Trim();
            existing.Quantity = req.Quantity;
            existing.Cost = req.Cost;
            existing.Price = req.Price;
            existing.ReorderLevel = req.ReorderLevel;
            existing.ReorderQty = req.ReorderQty;
            existing.IsActive = req.IsActive;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await container.ReplaceItemAsync(existing, id, new PartitionKey(pk));
            return Results.Ok(existing);
        });

        // DELETE /api/v5/products/{id}
        group.MapDelete("/{id}", async (string id, CosmosContainerFactory factory) =>
        {
            var container = factory.Products();
            var pk = CosmosContainerFactory.StorePk;

            try
            {
                await container.DeleteItemAsync<ProductV5>(id, new PartitionKey(pk));
                return Results.NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Results.NotFound();
            }
        });
    }
}