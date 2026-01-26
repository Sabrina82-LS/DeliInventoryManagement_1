using DeliInventoryManagement_1.Api.Data.Seed;
using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;

using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) Infra básica + Swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

// ✅ Boa prática (e evita dor de cabeça se tiver controllers no projeto)
builder.Services.AddControllers();

// =====================================================
// 2) Cosmos DB - Cliente Singleton
// =====================================================
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("CosmosDb");

    var endpoint = config["AccountEndpoint"] ?? config["Endpoint"];
    var key = config["AccountKey"] ?? config["Key"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException(
            "CosmosDb: configure AccountEndpoint/Endpoint e AccountKey/Key no appsettings.json.");

    return new CosmosClient(endpoint, key, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            // Mantém "Type" como "Type" (não vira "type"), compatível com PK "/Type"
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
        }
    });
});

// =====================================================
// 3) Dependency Injection - Services
// =====================================================
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

var app = builder.Build();

// =====================================================
// 4) Startup: garante DB + Container e executa Seed (DEV)
// =====================================================
await EnsureCosmosAndSeedAsync(app);

// =====================================================
// 5) Middlewares
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// =====================================================
// 6) Versionamento por URL
// =====================================================
const string productsV1 = "/api/v1";
const string categoriesV2 = "/api/v2";
const string suppliersV3 = "/api/v3";
const string salesV4 = "/api/v4";

// =====================================================
// 7) PRODUCTS (V1)
// =====================================================
app.MapGet($"{productsV1}/products", async (
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

    return Results.Ok(await service.GetProductsAsync(query));
})
.WithTags("1 - Products V1");

app.MapGet($"{productsV1}/products/{{id}}", async (string id, IProductService service)
    => await service.GetByIdAsync(id) is { } p ? Results.Ok(p) : Results.NotFound()
).WithTags("1 - Products V1");

app.MapPost($"{productsV1}/products", async (Product product, IProductService service)
    => Results.Created($"/products/{product.Id}", await service.CreateAsync(product))
).WithTags("1 - Products V1");

app.MapPut($"{productsV1}/products/{{id}}", async (string id, Product product, IProductService service)
    => await service.UpdateAsync(id, product) is { } p ? Results.Ok(p) : Results.NotFound()
).WithTags("1 - Products V1");

app.MapDelete($"{productsV1}/products/{{id}}", async (string id, IProductService service)
    => await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
).WithTags("1 - Products V1");

app.MapGet($"{productsV1}/products/summary", async (IProductService service)
    => Results.Ok(await service.GetSummaryAsync())
).WithTags("1 - Products V1");

// =====================================================
// 8) CATEGORIES (V2)
// =====================================================
app.MapGet($"{categoriesV2}/categories", async (ICategoryService s)
    => Results.Ok(await s.GetAllAsync())
).WithTags("2 - Categories V2");

app.MapGet($"{categoriesV2}/categories/{{id}}", async (string id, ICategoryService s)
    => await s.GetByIdAsync(id) is { } c ? Results.Ok(c) : Results.NotFound()
).WithTags("2 - Categories V2");

app.MapPost($"{categoriesV2}/categories", async (Category c, ICategoryService s)
    => Results.Created($"/categories/{c.Id}", await s.CreateAsync(c))
).WithTags("2 - Categories V2");

app.MapPut($"{categoriesV2}/categories/{{id}}", async (string id, Category c, ICategoryService s)
    => await s.UpdateAsync(id, c) is { } r ? Results.Ok(r) : Results.NotFound()
).WithTags("2 - Categories V2");

app.MapDelete($"{categoriesV2}/categories/{{id}}", async (string id, ICategoryService s)
    => await s.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
).WithTags("2 - Categories V2");

// =====================================================
// 9) SUPPLIERS (V3)
// =====================================================
app.MapGet($"{suppliersV3}/suppliers", async (ISupplierService s)
    => Results.Ok(await s.GetAllAsync())
).WithTags("3 - Suppliers V3");

app.MapGet($"{suppliersV3}/suppliers/{{id}}", async (string id, ISupplierService s)
    => await s.GetByIdAsync(id) is { } sp ? Results.Ok(sp) : Results.NotFound()
).WithTags("3 - Suppliers V3");

app.MapPost($"{suppliersV3}/suppliers", async (Supplier s, ISupplierService svc)
    => Results.Created($"/suppliers/{s.Id}", await svc.CreateAsync(s))
).WithTags("3 - Suppliers V3");

app.MapPut($"{suppliersV3}/suppliers/{{id}}", async (string id, Supplier s, ISupplierService svc)
    => await svc.UpdateAsync(id, s) is { } r ? Results.Ok(r) : Results.NotFound()
).WithTags("3 - Suppliers V3");

app.MapDelete($"{suppliersV3}/suppliers/{{id}}", async (string id, ISupplierService svc)
    => await svc.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
).WithTags("3 - Suppliers V3");

// =====================================================
// 10) SALES (V4)
// =====================================================
app.MapGet($"{salesV4}/sales", async (CosmosClient cosmos, IConfiguration cfg) =>
{
    var c = cfg.GetSection("CosmosDb");
    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";

    if (string.IsNullOrWhiteSpace(dbId))
        return Results.Problem("CosmosDb: DatabaseId não configurado.");

    if (string.IsNullOrWhiteSpace(containerId))
        containerId = "Items";

    var container = cosmos.GetContainer(dbId, containerId);

    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.Type = 'Sale' AND IS_DEFINED(c.CreatedAtUtc) ORDER BY c.CreatedAtUtc DESC"
    );

    var iterator = container.GetItemQueryIterator<Sales>(
        query,
        requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey("Sale")
        });

    var results = new List<Sales>();

    while (iterator.HasMoreResults)
    {
        var page = await iterator.ReadNextAsync();
        results.AddRange(page);
    }

    return Results.Ok(results);
})
.WithTags("4 - Sales V4");

app.MapPost($"{salesV4}/sales", async (
    CreateSaleRequest req,
    CosmosClient cosmos,
    IConfiguration cfg) =>
{
    try
    {
        if (req is null)
            return Results.BadRequest("Body is required.");

        var productId = req.ProductId?.Trim();

        if (string.IsNullOrWhiteSpace(productId))
            return Results.BadRequest("ProductId is required.");

        if (req.Quantity <= 0)
            return Results.BadRequest("Quantity must be greater than 0.");

        var c = cfg.GetSection("CosmosDb");
        var dbId = c["DatabaseId"] ?? c["DatabaseName"];
        var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";

        if (string.IsNullOrWhiteSpace(dbId))
            return Results.Problem("CosmosDb: DatabaseId não configurado.");

        if (string.IsNullOrWhiteSpace(containerId))
            containerId = "Items";

        var container = cosmos.GetContainer(dbId, containerId);

        // 1) Buscar o produto (PK = "Product")
        Product product;
        try
        {
            var resp = await container.ReadItemAsync<Product>(
                id: productId,
                partitionKey: new PartitionKey("Product"));

            product = resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound($"Product '{productId}' not found.");
        }

        if (product.Quantity < req.Quantity)
            return Results.BadRequest($"Insufficient stock. Available: {product.Quantity}, requested: {req.Quantity}.");

        // 2) Criar a venda
        var sale = new Sales
        {
            Id = Guid.NewGuid().ToString(),
            Type = "Sale",
            ProductId = product.Id,
            ProductName = product.Name,
            CategoryId = product.CategoryId,
            Quantity = req.Quantity,
            UnitPrice = product.Price,
            Total = product.Price * req.Quantity,
            CreatedAtUtc = DateTime.UtcNow
        };

        // 3) Salvar a venda (PK = "Sale")
        await container.CreateItemAsync(sale, new PartitionKey("Sale"));

        return Results.Created($"{salesV4}/sales/{sale.Id}", sale);
    }
    catch (CosmosException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "CosmosException while creating sale",
            statusCode: (int)ex.StatusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Unhandled error while creating sale",
            statusCode: 500);
    }
})
.WithTags("4 - Sales V4");

app.Run();

// =====================================================
// Helper: cria DB/Container e roda SeedRunner
// (Protegido: não derruba a API se seed falhar)
// =====================================================
static async Task EnsureCosmosAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();

    var c = cfg.GetSection("CosmosDb");

    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";
    var pkPath = c["PartitionKeyPath"] ?? "/Type";

    if (string.IsNullOrWhiteSpace(dbId))
        throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

    var db = await cosmos.CreateDatabaseIfNotExistsAsync(dbId);
    await db.Database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(containerId, pkPath)
    );

    if (env.IsDevelopment())
    {
        try
        {
            var container = cosmos.GetContainer(dbId, containerId);
            await SeedRunner.RunAsync(container, env);
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ EnsureCosmosAndSeedAsync: seed falhou, mas a API vai continuar.");
            Console.WriteLine(ex);
        }
    }
}
