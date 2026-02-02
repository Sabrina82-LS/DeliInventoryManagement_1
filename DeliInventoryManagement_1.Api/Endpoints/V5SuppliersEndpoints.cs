using DeliInventoryManagement_1.Api.Dtos.V5;
using DeliInventoryManagement_1.Api.Models.V5;
using Microsoft.Azure.Cosmos;

namespace DeliInventoryManagement_1.Api.Endpoints.V5;

public static class V5SuppliersEndpoints
{
    public static void MapV5Suppliers(this WebApplication app)
    {
        var group = app.MapGroup("/api/v5/suppliers")
            .WithTags("Suppliers V5");

        // GET /api/v5/suppliers
        group.MapGet("", async (CosmosClient cosmos, IConfiguration cfg) =>
        {
            var container = GetSuppliersContainer(cosmos, cfg);

            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type ORDER BY c.name"
            ).WithParameter("@type", "Supplier");

            var it = container.GetItemQueryIterator<SupplierV5>(
                q,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey("supplier")
                });

            var results = new List<SupplierV5>();
            while (it.HasMoreResults)
            {
                var page = await it.ReadNextAsync();
                results.AddRange(page);
            }

            return Results.Ok(results);
        });

        // GET /api/v5/suppliers/{id}
        group.MapGet("{id}", async (string id, CosmosClient cosmos, IConfiguration cfg) =>
        {
            var container = GetSuppliersContainer(cosmos, cfg);

            try
            {
                var resp = await container.ReadItemAsync<SupplierV5>(
                    id,
                    new PartitionKey("supplier"));

                return Results.Ok(resp.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound(new { message = "Supplier not found." });
            }
        });

        // POST /api/v5/suppliers
        group.MapPost("", async (CreateSupplierRequest req, CosmosClient cosmos, IConfiguration cfg) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { message = "Name is required." });

            var container = GetSuppliersContainer(cosmos, cfg);

            var supplier = new SupplierV5
            {
                Id = Guid.NewGuid().ToString("n"),
                Pk = "supplier",
                Type = "Supplier",
                Name = req.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
                Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await container.CreateItemAsync(supplier, new PartitionKey(supplier.Pk));

            return Results.Created($"/api/v5/suppliers/{supplier.Id}", supplier);
        });

        // PUT /api/v5/suppliers/{id}
        group.MapPut("{id}", async (string id, UpdateSupplierRequest req, CosmosClient cosmos, IConfiguration cfg) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { message = "Name is required." });

            var container = GetSuppliersContainer(cosmos, cfg);

            SupplierV5 existing;
            try
            {
                var read = await container.ReadItemAsync<SupplierV5>(id, new PartitionKey("supplier"));
                existing = read.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound(new { message = "Supplier not found." });
            }

            existing.Name = req.Name.Trim();
            existing.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            existing.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            existing.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await container.ReplaceItemAsync(existing, existing.Id, new PartitionKey(existing.Pk));

            return Results.Ok(existing);
        });

        // DELETE /api/v5/suppliers/{id}
        group.MapDelete("{id}", async (string id, CosmosClient cosmos, IConfiguration cfg) =>
        {
            var container = GetSuppliersContainer(cosmos, cfg);

            try
            {
                await container.DeleteItemAsync<SupplierV5>(id, new PartitionKey("supplier"));
                return Results.NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound(new { message = "Supplier not found." });
            }
        });
    }

    private static Container GetSuppliersContainer(CosmosClient cosmos, IConfiguration cfg)
    {
        var c = cfg.GetSection("CosmosDb");

        // Mantém compatibilidade com teus nomes antigos
        var dbId = c["DatabaseId"] ?? c["DatabaseName"];
        var containerId = c["SuppliersContainerId"] ?? "Suppliers";

        if (string.IsNullOrWhiteSpace(dbId))
            throw new InvalidOperationException("CosmosDb:DatabaseId (ou DatabaseName) não configurado.");

        return cosmos.GetContainer(dbId, containerId);
    }
}
