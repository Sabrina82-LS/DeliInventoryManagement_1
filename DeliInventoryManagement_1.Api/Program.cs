using System.Text.Json;
using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache
builder.Services.AddMemoryCache();

// Cosmos client (singleton)
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("CosmosDb");

    // ✅ Compatível com os nomes que você já usou:
    // AccountEndpoint / AccountKey
    // ou Endpoint / Key (caso você use esses)
    var endpoint = config["AccountEndpoint"] ?? config["Endpoint"];
    var key = config["AccountKey"] ?? config["Key"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("CosmosDb: 'AccountEndpoint/Endpoint' e 'AccountKey/Key' não configurados no appsettings.json.");

    var cosmosClientOptions = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            // ✅ Mantém exatamente o nome das propriedades (Type, Name, CategoryId, ...)
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
        }
    };

    return new CosmosClient(endpoint, key, cosmosClientOptions);
});

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

var app = builder.Build();

// ✅ Cria DB + Container automaticamente no startup
// e faz seed automático de 50 produtos (apenas DEV e se estiver vazio)
using (var scope = app.Services.CreateScope())
{
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();

    var c = cfg.GetSection("CosmosDb");

    // ✅ Compatível com DatabaseId/ContainerId e DatabaseName/ContainerName
    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";

    // Recomendo /Type para container com múltiplas entidades
    var partitionKeyPath = c["PartitionKeyPath"] ?? "/Type";

    if (string.IsNullOrWhiteSpace(dbId))
        throw new InvalidOperationException("CosmosDb: 'DatabaseId' (ou 'DatabaseName') não configurado no appsettings.json.");

    // 1) cria DB
    var dbResp = await cosmos.CreateDatabaseIfNotExistsAsync(dbId);

    // 2) cria Container
    await dbResp.Database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(containerId, partitionKeyPath)
    );

    // 3) Seed automático (somente DEV e se não houver Products)
    if (env.IsDevelopment())
    {
        var container = cosmos.GetContainer(dbId, containerId);

        // Conta Products existentes
        var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.Type = @type")
            .WithParameter("@type", "Product");

        using var it = container.GetItemQueryIterator<int>(countQuery);
        var productCount = 0;

        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync();
            productCount += resp.Resource.FirstOrDefault();
        }

        if (productCount == 0)
        {
            // Categorias simples para os 50 produtos
            var categories = new[]
            {
                new { Id = "c1", Name = "Meat" },
                new { Id = "c2", Name = "Dairy" },
                new { Id = "c3", Name = "Beverages" },
                new { Id = "c4", Name = "Bakery" },
                new { Id = "c5", Name = "Vegetables" },
            };

            var rnd = new Random();

            for (int i = 1; i <= 50; i++)
            {
                var cat = categories[rnd.Next(categories.Length)];

                var cost = Math.Round((decimal)(rnd.NextDouble() * 20 + 1), 2); // 1.00..21.00
                var price = Math.Round(cost * (decimal)(1.25 + rnd.NextDouble() * 0.75), 2); // margem 25%..100%

                var product = new Product
                {
                    Id = $"p{i}",               // p1..p50 (facilita)
                    Type = "Product",
                    Name = $"Product {i}",
                    CategoryId = cat.Id,
                    CategoryName = cat.Name,
                    Quantity = rnd.Next(0, 200),
                    Cost = cost,
                    Price = price,
                    ReorderLevel = rnd.Next(3, 15)
                };

                // PartitionKey = product.Type (porque /Type)
                await container.UpsertItemAsync(product, new PartitionKey(product.Type));
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Simple API versioning via URL segment: /api/v1/...
var productsV1Prefix = "/api/v1";
var categoriesV2Prefix = "/api/v2";
var suppliersV3Prefix = "/api/v3";


// ========== PRODUCT ENDPOINTS (V1) ==========
app.MapGet($"{productsV1Prefix}/products", async (
    string? search,
    string? categoryId,
    string? sortBy,
    string? sortDir,
    int? page,
    int? pageSize,
    IProductService service) =>
{
    var query = new ProductQueryParameters
    {
        Search = search,
        CategoryId = categoryId,
        SortBy = sortBy ?? "Name",
        SortDir = sortDir ?? "asc",
        Page = page ?? 1,
        PageSize = pageSize ?? 10
    };

    var result = await service.GetProductsAsync(query);
    return Results.Ok(result);
})
.WithName("GetProducts")
.WithTags("Products V1")
.Produces<PagedResult<Product>>(StatusCodes.Status200OK);

app.MapGet($"{productsV1Prefix}/products/{{id}}", async (string id, IProductService service) =>
{
    var product = await service.GetByIdAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProductById")
.WithTags("Products V1")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost($"{productsV1Prefix}/products", async (Product product, IProductService service) =>
{
    var created = await service.CreateAsync(product);
    return Results.CreatedAtRoute("GetProductById", new { id = created.Id }, created);
})
.WithName("CreateProduct")
.WithTags("Products V1")
.Produces<Product>(StatusCodes.Status201Created);

app.MapPut($"{productsV1Prefix}/products/{{id}}", async (string id, Product product, IProductService service) =>
{
    var updated = await service.UpdateAsync(id, product);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
})
.WithName("UpdateProduct")
.WithTags("Products V1")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete($"{productsV1Prefix}/products/{{id}}", async (string id, IProductService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteProduct")
.WithTags("Products V1")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet($"{productsV1Prefix}/products/summary", async (IProductService service) =>
{
    var summary = await service.GetSummaryAsync();
    return Results.Ok(summary);
})
.WithName("GetProductSummary")
.WithTags("Products V1")
.Produces<ProductSummary>(StatusCodes.Status200OK);


// ========== CATEGORY ENDPOINTS (V2) ==========
app.MapGet($"{categoriesV2Prefix}/categories", async (ICategoryService service) =>
{
    var items = await service.GetAllAsync();
    return Results.Ok(items);
})
.WithName("GetCategories")
.WithTags("Categories V2")
.Produces<IReadOnlyList<Category>>(StatusCodes.Status200OK);

app.MapGet($"{categoriesV2Prefix}/categories/{{id}}", async (string id, ICategoryService service) =>
{
    var item = await service.GetByIdAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
.WithName("GetCategoryById")
.WithTags("Categories V2")
.Produces<Category>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost($"{categoriesV2Prefix}/categories", async (Category category, ICategoryService service) =>
{
    var created = await service.CreateAsync(category);
    return Results.CreatedAtRoute("GetCategoryById", new { id = created.Id }, created);
})
.WithName("CreateCategory")
.WithTags("Categories V2")
.Produces<Category>(StatusCodes.Status201Created);

app.MapPut($"{categoriesV2Prefix}/categories/{{id}}", async (string id, Category category, ICategoryService service) =>
{
    var updated = await service.UpdateAsync(id, category);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
})
.WithName("UpdateCategory")
.WithTags("Categories V2")
.Produces<Category>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete($"{categoriesV2Prefix}/categories/{{id}}", async (string id, ICategoryService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteCategory")
.WithTags("Categories V2")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);


// ========== SUPPLIER ENDPOINTS (V3) ==========
app.MapGet($"{suppliersV3Prefix}/suppliers", async (ISupplierService service) =>
{
    var items = await service.GetAllAsync();
    return Results.Ok(items);
})
.WithName("GetSuppliers")
.WithTags("Suppliers V3")
.Produces<IReadOnlyList<Supplier>>(StatusCodes.Status200OK);

app.MapGet($"{suppliersV3Prefix}/suppliers/{{id}}", async (string id, ISupplierService service) =>
{
    var item = await service.GetByIdAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
})
.WithName("GetSupplierById")
.WithTags("Suppliers V3")
.Produces<Supplier>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost($"{suppliersV3Prefix}/suppliers", async (Supplier supplier, ISupplierService service) =>
{
    var created = await service.CreateAsync(supplier);
    return Results.CreatedAtRoute("GetSupplierById", new { id = created.Id }, created);
})
.WithName("CreateSupplier")
.WithTags("Suppliers V3")
.Produces<Supplier>(StatusCodes.Status201Created);

app.MapPut($"{suppliersV3Prefix}/suppliers/{{id}}", async (string id, Supplier supplier, ISupplierService service) =>
{
    var updated = await service.UpdateAsync(id, supplier);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
})
.WithName("UpdateSupplier")
.WithTags("Suppliers V3")
.Produces<Supplier>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete($"{suppliersV3Prefix}/suppliers/{{id}}", async (string id, ISupplierService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteSupplier")
.WithTags("Suppliers V3")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);


// ✅ Seu endpoint de seed manual (mantido)
app.MapPost("/api/test/seed/products15", async (IConfiguration cfg, CosmosClient cosmos, IWebHostEnvironment env) =>
{
    if (!env.IsDevelopment())
        return Results.NotFound();

    var c = cfg.GetSection("CosmosDb");

    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Inventory";
    var pkPath = c["PartitionKeyPath"] ?? "/Type";

    if (string.IsNullOrWhiteSpace(dbId) || string.IsNullOrWhiteSpace(containerId))
        return Results.BadRequest("Configure CosmosDb: DatabaseId e ContainerId no appsettings.json.");

    var dbResp = await cosmos.CreateDatabaseIfNotExistsAsync(dbId);
    await dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(containerId, pkPath));

    var container = cosmos.GetContainer(dbId, containerId);

    // 🔥 NOVO: apagar todos os auto-* existentes
    var query = new QueryDefinition(
        "SELECT c.id FROM c WHERE c.Type = @type AND STARTSWITH(c.id, @prefix)")
        .WithParameter("@type", "Product")
        .WithParameter("@prefix", "auto-");

    using (var it = container.GetItemQueryIterator<dynamic>(query))
    {
        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync();
            foreach (var doc in resp)
            {
                string id = doc.id;
                await container.DeleteItemAsync<dynamic>(id, new PartitionKey("Product"));
            }
        }
    }

    var categories = new[]
    {
        new { Id = "c1", Name = "Meat" },
        new { Id = "c2", Name = "Dairy" },
        new { Id = "c3", Name = "Beverages" },
        new { Id = "c4", Name = "Bakery" },
        new { Id = "c5", Name = "Vegetables" },
        new { Id = "c6", Name = "Test" },
    };

    var productNames = new[]
    {
        "Ham",
        "Chicken Breast",
        "Ground Beef",
        "Milk 1L",
        "Cheddar Cheese",
        "Butter",
        "Natural Yogurt",
        "Orange Juice",
        "Mineral Water 1.5L",
        "Coca-Cola 2L",
        "Baguette",
        "Whole Wheat Bread",
        "Eggs (12 pack)",
        "Tomatoes",
        "Potatoes"
    };

    var rnd = new Random();

    // ✅ recriar auto-1..auto-15 com nomes reais
    for (int i = 1; i <= 15; i++)
    {
        var cat = categories[rnd.Next(categories.Length)];
        var cost = Math.Round((decimal)(rnd.NextDouble() * 20 + 1), 2);
        var price = Math.Round(cost * (decimal)(1.25 + rnd.NextDouble() * 0.75), 2);

        var product = new Product
        {
            Id = $"auto-{i}",
            Type = "Product",
            Name = productNames[i - 1],
            CategoryId = cat.Id,
            CategoryName = cat.Name,
            Quantity = rnd.Next(0, 200),
            Cost = cost,
            Price = price,
            ReorderLevel = 5
        };

        await container.UpsertItemAsync(product, new PartitionKey(product.Type));
    }

    return Results.Ok(new
    {
        message = "Auto Products recriados com nomes reais!",
        total = 15,
        database = dbId,
        container = containerId
    });
})
.WithName("SeedProducts15")
.WithTags("Test");


app.Run();

