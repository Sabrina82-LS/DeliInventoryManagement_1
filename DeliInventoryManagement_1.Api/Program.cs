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
    var endpoint = config["AccountEndpoint"]!;
    var key = config["AccountKey"]!;
    var cosmosClientOptions = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
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


// ========== PRODUCT ENDPOINTS ==========

// GET /api/v1/products  (search, filter, sort, paging)
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


// ========== CATEGORY ENDPOINTS ==========
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



// TODO: add similar endpoints for Categories & Suppliers here.


app.MapPost("/api/test/seed", async (IConfiguration cfg, CosmosClient cosmos, IWebHostEnvironment env) =>
{
    // Segurança: endpoint de seed só em Development
    if (!env.IsDevelopment())
        return Results.NotFound();

    try
    {
        var c = cfg.GetSection("CosmosDb");

        var dbId = c["DatabaseId"];
        var inventoryContainerId = c["ContainerId"]; // ex: Inventory
        var suppliersContainerId = c["SuppliersContainerId"]; // ex: Suppliers (opcional)
        var partitionKeyPath = c["PartitionKeyPath"] ?? "/Type";

        if (string.IsNullOrWhiteSpace(dbId) || string.IsNullOrWhiteSpace(inventoryContainerId))
            return Results.BadRequest("CosmosDb: 'DatabaseId' e/ou 'ContainerId' não configurados no appsettings.json.");

        // Se não definires SuppliersContainerId, usa o mesmo ContainerId
        suppliersContainerId ??= inventoryContainerId;

        var db = cosmos.GetDatabase(dbId);

        // Garante containers (cria se não existir)
        await db.CreateContainerIfNotExistsAsync(new ContainerProperties(inventoryContainerId, partitionKeyPath));
        await db.CreateContainerIfNotExistsAsync(new ContainerProperties(suppliersContainerId, partitionKeyPath));

        var inventory = db.GetContainer(inventoryContainerId);
        var suppliers = db.GetContainer(suppliersContainerId);

        // Executa o seed
        await DeliInventoryManagement_1.Api.Tests.SeedTestData.RunAsync(inventory, suppliers);

        return Results.Ok(new
        {
            message = "Seed completed successfully",
            database = dbId,
            inventoryContainer = inventoryContainerId,
            suppliersContainer = suppliersContainerId,
            suppliersInserted = 2,
            productsInserted = 15
        });
    }
    catch (CosmosException ex)
    {
        return Results.Problem(
            title: "Cosmos DB error during seed",
            detail: ex.Message,
            statusCode: (int)ex.StatusCode
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Unexpected error during seed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("SeedTestData")
.WithTags("Test");

app.Run();
