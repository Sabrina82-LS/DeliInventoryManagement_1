using DeliInventoryManagement_1.Api.Data.Seed;
using DeliInventoryManagement_1.Api.Dtos;
using DeliInventoryManagement_1.Api.Models;
using DeliInventoryManagement_1.Api.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) Infra básica + Swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddControllers();

// ✅ JSON para Minimal APIs (Swagger/body em camelCase)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// ✅ PASSO 2 — CORS (Blazor -> API)
// =====================================================
// Ajuste a porta se seu Blazor não for 7081.
const string blazorHttps = "https://localhost:7081";
const string blazorHttp = "http://localhost:7081";

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCors", policy =>
        policy
            .WithOrigins(blazorHttps, blazorHttp)
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

// =====================================================
// 2) Cosmos DB - Cliente Singleton
//    ✅ Força serializer System.Text.Json para respeitar [JsonPropertyName("id")]
// =====================================================
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("CosmosDb");

    var endpoint = config["AccountEndpoint"] ?? config["Endpoint"];
    var key = config["AccountKey"] ?? config["Key"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException(
            "CosmosDb: configure AccountEndpoint/Endpoint e AccountKey/Key no appsettings.json.");

    var stjOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
        // Mantemos sem camelCase aqui; quem manda são os [JsonPropertyName(...)]
    };

    return new CosmosClient(endpoint, key, new CosmosClientOptions
    {
        Serializer = new CosmosStjSerializer(stjOptions)
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

// ✅ CORS precisa vir ANTES dos endpoints
app.UseCors("BlazorCors");

// (opcional, mas recomendado com controllers)
app.UseAuthorization();

app.MapControllers();

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

// POST /api/v4/sales
app.MapPost($"{salesV4}/sales", async (
    CosmosClient cosmos,
    IConfiguration cfg,
    CreateSaleRequest req) =>
{
    var c = cfg.GetSection("CosmosDb");
    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";

    if (string.IsNullOrWhiteSpace(dbId))
        return Results.Problem("CosmosDb DatabaseId/DatabaseName não configurado.");

    if (string.IsNullOrWhiteSpace(containerId))
        return Results.Problem("CosmosDb ContainerId/ContainerName não configurado.");

    var container = cosmos.GetContainer(dbId!, containerId);

    if (req is null)
        return Results.BadRequest("Body is required.");

    if (req.Lines == null || req.Lines.Count == 0)
        return Results.BadRequest("Sale must contain at least one line.");

    var groupedLines = req.Lines
        .Select(l => new
        {
            ProductId = (l.ProductId ?? "").Trim(),
            ProductName = l.ProductName,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        })
        .Where(l => !string.IsNullOrWhiteSpace(l.ProductId) && l.Quantity > 0)
        .GroupBy(l => l.ProductId)
        .Select(g => new
        {
            ProductId = g.Key,
            Quantity = g.Sum(x => x.Quantity),
            ProductName = g.First().ProductName,
            UnitPrice = g.First().UnitPrice
        })
        .ToList();

    if (groupedLines.Count == 0)
        return Results.BadRequest("Sale lines are invalid (missing productId or quantity <= 0).");

    var productsById = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);

    // 1) Verificar stock
    foreach (var line in groupedLines)
    {
        try
        {
            var resp = await container.ReadItemAsync<Product>(
                id: line.ProductId,
                partitionKey: new PartitionKey("Product"));

            var product = resp.Resource;
            productsById[line.ProductId] = product;

            if (product.Quantity < line.Quantity)
                return Results.BadRequest(
                    $"Not enough stock for '{product.Name}'. Available={product.Quantity}, Requested={line.Quantity}");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound($"Product '{line.ProductId}' not found.");
        }
        catch (CosmosException ex)
        {
            return Results.Problem(
                detail: $"Cosmos error while reading product '{line.ProductId}': {ex.Message}",
                statusCode: (int)ex.StatusCode);
        }
    }

    // 2) Montar Sale
    var sale = new Sales
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "Sale",
        Date = req.Date == default ? DateTime.UtcNow : req.Date,
        CreatedAtUtc = DateTime.UtcNow,
        Lines = groupedLines.Select(l => new SaleLine
        {
            ProductId = l.ProductId,
            ProductName = string.IsNullOrWhiteSpace(l.ProductName)
                ? productsById[l.ProductId].Name
                : l.ProductName!,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList(),
        Total = groupedLines.Sum(l => l.Quantity * l.UnitPrice)
    };

    // 3) Criar Sale primeiro
    try
    {
        await container.CreateItemAsync(sale, new PartitionKey("Sale"));
    }
    catch (CosmosException ex)
    {
        return Results.Problem(
            detail: $"Cosmos error while creating sale: {ex.Message}",
            statusCode: (int)ex.StatusCode);
    }

    // 4) Baixar stock (no seu Cosmos é "Quantity" com Q maiúsculo)
    foreach (var line in groupedLines)
    {
        try
        {
            await container.PatchItemAsync<dynamic>(
                id: line.ProductId,
                partitionKey: new PartitionKey("Product"),
                patchOperations: new[]
                {
                    PatchOperation.Increment("/Quantity", -line.Quantity)
                });
        }
        catch (CosmosException ex)
        {
            return Results.Problem(
                detail: $"Sale '{sale.Id}' created, but failed to patch product '{line.ProductId}': {ex.Message}",
                statusCode: (int)ex.StatusCode);
        }
    }

    return Results.Created($"{salesV4}/sales/{sale.Id}", new { saleId = sale.Id, sale.Total });
})
.WithTags("4 - Sales V4");


// =====================================================
// ✅ ENDPOINT POST /restocks
//    - Cria um documento Restock (Type = "Restock")
//    - Aumenta o stock do(s) Product(s) usando PATCH Increment (+)
// =====================================================
app.MapPost("/api/restocks", async (
    CosmosClient cosmos,
    IConfiguration cfg,
    CreateRestockRequest req) =>
{
    var c = cfg.GetSection("CosmosDb");
    var dbId = c["DatabaseId"] ?? c["DatabaseName"];
    var containerId = c["ContainerId"] ?? c["ContainerName"] ?? "Items";

    if (string.IsNullOrWhiteSpace(dbId))
        return Results.Problem("CosmosDb DatabaseId/DatabaseName não configurado.");

    if (string.IsNullOrWhiteSpace(containerId))
        return Results.Problem("CosmosDb ContainerId/ContainerName não configurado.");

    var container = cosmos.GetContainer(dbId!, containerId);

    if (req is null)
        return Results.BadRequest("Body is required.");

    if (req.Lines == null || req.Lines.Count == 0)
        return Results.BadRequest("Restock must contain at least one line.");

    // 1) Normaliza e agrupa linhas por ProductId (evita duplicados)
    var groupedLines = req.Lines
        .Select(l => new
        {
            ProductId = (l.ProductId ?? "").Trim(),
            ProductName = l.ProductName,
            Quantity = l.Quantity,
            CostPerUnit = l.CostPerUnit
        })
        .Where(l => !string.IsNullOrWhiteSpace(l.ProductId) && l.Quantity > 0)
        .GroupBy(l => l.ProductId)
        .Select(g => new
        {
            ProductId = g.Key,
            Quantity = g.Sum(x => x.Quantity),
            ProductName = g.First().ProductName,
            CostPerUnit = g.First().CostPerUnit
        })
        .ToList();

    if (groupedLines.Count == 0)
        return Results.BadRequest("Restock lines are invalid (missing productId or quantity <= 0).");

    // 2) (Opcional, mas recomendado) validar se o produto existe
    var productsById = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);

    foreach (var line in groupedLines)
    {
        try
        {
            var resp = await container.ReadItemAsync<Product>(
                id: line.ProductId,
                partitionKey: new PartitionKey("Product"));

            productsById[line.ProductId] = resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound($"Product '{line.ProductId}' not found.");
        }
        catch (CosmosException ex)
        {
            return Results.Problem(
                detail: $"Cosmos error while reading product '{line.ProductId}': {ex.Message}",
                statusCode: (int)ex.StatusCode);
        }
    }

    // 3) Montar Restock
    var restock = new Restock
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "Restock",
        Date = req.Date == default ? DateTime.UtcNow : req.Date,
        SupplierId = req.SupplierId,
        SupplierName = req.SupplierName,
        CreatedAtUtc = DateTime.UtcNow,
        Lines = groupedLines.Select(l => new RestockLine
        {
            ProductId = l.ProductId,
            ProductName = string.IsNullOrWhiteSpace(l.ProductName)
                ? productsById[l.ProductId].Name
                : l.ProductName!,
            Quantity = l.Quantity,
            CostPerUnit = l.CostPerUnit
        }).ToList(),
    };

    restock.TotalCost = restock.Lines.Sum(l => l.Quantity * l.CostPerUnit);

    // 4) Criar Restock primeiro
    try
    {
        await container.CreateItemAsync(restock, new PartitionKey("Restock"));
    }
    catch (CosmosException ex)
    {
        return Results.Problem(
            detail: $"Cosmos error while creating restock: {ex.Message}",
            statusCode: (int)ex.StatusCode);
    }

    // 5) Subir stock com PATCH Increment (+Quantity)
    foreach (var line in groupedLines)
    {
        try
        {
            await container.PatchItemAsync<dynamic>(
                id: line.ProductId,
                partitionKey: new PartitionKey("Product"),
                patchOperations: new[]
                {
                    PatchOperation.Increment("/Quantity", line.Quantity)
                });
        }
        catch (CosmosException ex)
        {
            return Results.Problem(
                detail: $"Restock '{restock.Id}' created, but failed to patch product '{line.ProductId}': {ex.Message}",
                statusCode: (int)ex.StatusCode);
        }
    }

    return Results.Created($"/api/restocks/{restock.Id}", new { restockId = restock.Id, restock.TotalCost });
})
.WithTags("5 - Restocks");

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

// =====================================================
// Cosmos Serializer (System.Text.Json)
// =====================================================
sealed class CosmosStjSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosStjSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public override T FromStream<T>(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
            return (T)(object)stream;

        using var sr = new StreamReader(stream);
        var json = sr.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }

    public override Stream ToStream<T>(T input)
    {
        var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, leaveOpen: true);
        var json = JsonSerializer.Serialize(input, _options);
        sw.Write(json);
        sw.Flush();
        ms.Position = 0;
        return ms;
    }
}
