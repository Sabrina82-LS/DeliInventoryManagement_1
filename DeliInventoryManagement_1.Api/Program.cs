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

// Cache em memória (opcional, mas útil)
builder.Services.AddMemoryCache();

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
            // Mantém nomes exatos das propriedades (Type, Name, CategoryId, etc.)
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
.WithTags("Products V1");

app.MapGet($"{productsV1}/products/{{id}}", async (string id, IProductService service)
    => await service.GetByIdAsync(id) is { } p ? Results.Ok(p) : Results.NotFound()
);

app.MapPost($"{productsV1}/products", async (Product product, IProductService service)
    => Results.Created($"/products/{product.Id}", await service.CreateAsync(product))
);

app.MapPut($"{productsV1}/products/{{id}}", async (string id, Product product, IProductService service)
    => await service.UpdateAsync(id, product) is { } p ? Results.Ok(p) : Results.NotFound()
);

app.MapDelete($"{productsV1}/products/{{id}}", async (string id, IProductService service)
    => await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
);

app.MapGet($"{productsV1}/products/summary", async (IProductService service)
    => Results.Ok(await service.GetSummaryAsync())
);

// =====================================================
// 8) CATEGORIES (V2)
// =====================================================
app.MapGet($"{categoriesV2}/categories", async (ICategoryService s)
    => Results.Ok(await s.GetAllAsync())
);

app.MapGet($"{categoriesV2}/categories/{{id}}", async (string id, ICategoryService s)
    => await s.GetByIdAsync(id) is { } c ? Results.Ok(c) : Results.NotFound()
);

app.MapPost($"{categoriesV2}/categories", async (Category c, ICategoryService s)
    => Results.Created($"/categories/{c.Id}", await s.CreateAsync(c))
);

app.MapPut($"{categoriesV2}/categories/{{id}}", async (string id, Category c, ICategoryService s)
    => await s.UpdateAsync(id, c) is { } r ? Results.Ok(r) : Results.NotFound()
);

app.MapDelete($"{categoriesV2}/categories/{{id}}", async (string id, ICategoryService s)
    => await s.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
);

// =====================================================
// 9) SUPPLIERS (V3)
// =====================================================
app.MapGet($"{suppliersV3}/suppliers", async (ISupplierService s)
    => Results.Ok(await s.GetAllAsync())
);

app.MapGet($"{suppliersV3}/suppliers/{{id}}", async (string id, ISupplierService s)
    => await s.GetByIdAsync(id) is { } sp ? Results.Ok(sp) : Results.NotFound()
);

app.MapPost($"{suppliersV3}/suppliers", async (Supplier s, ISupplierService svc)
    => Results.Created($"/suppliers/{s.Id}", await svc.CreateAsync(s))
);

app.MapPut($"{suppliersV3}/suppliers/{{id}}", async (string id, Supplier s, ISupplierService svc)
    => await svc.UpdateAsync(id, s) is { } r ? Results.Ok(r) : Results.NotFound()
);

app.MapDelete($"{suppliersV3}/suppliers/{{id}}", async (string id, ISupplierService svc)
    => await svc.DeleteAsync(id) ? Results.NoContent() : Results.NotFound()
);

app.Run();

// =====================================================
// Helper: cria DB/Container e roda SeedRunner
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
        var container = cosmos.GetContainer(dbId, containerId);
        await SeedRunner.RunAsync(container, env);
    }
}
