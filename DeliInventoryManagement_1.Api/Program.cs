using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Endpoints;
using DeliInventoryManagement_1.Api.Endpoints.V5;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Services.Outbox; // 👈 PASSO 10.2
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) Infra básica + Swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Se ainda existir algum controller legado, manter
builder.Services.AddControllers();

builder.Services.AddMemoryCache();

// ✅ JSON para Minimal APIs (camelCase)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// 2) CORS (Blazor -> API)
// =====================================================
const string blazorHttps = "https://localhost:7081";
const string blazorHttp = "http://localhost:7081";

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCors", policy =>
        policy.WithOrigins(blazorHttps, blazorHttp)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// =====================================================
// 3) Authorization
// =====================================================
builder.Services.AddAuthorization();

// =====================================================
// 4) Cosmos Options + CosmosClient + ContainerFactory
// =====================================================
builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.AccountEndpoint) ||
        string.IsNullOrWhiteSpace(opt.AccountKey))
    {
        throw new InvalidOperationException(
            "CosmosDb: configure AccountEndpoint e AccountKey no appsettings.json.");
    }

    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    return new CosmosClient(
        opt.AccountEndpoint,
        opt.AccountKey,
        new CosmosClientOptions
        {
            Serializer = new CosmosStjSerializer(jsonOptions)
        });
});

builder.Services.AddSingleton<CosmosContainerFactory>();

// =====================================================
// 🔥 PASSO 10.2 — OUTBOX DISPATCHER (BACKGROUND SERVICE)
// =====================================================
builder.Services.AddHostedService<OutboxDispatcherV5>();

var app = builder.Build();

// =====================================================
// 5) Startup: garante DB + Containers V5 (schema híbrido /pk)
// =====================================================
await EnsureCosmosSchemaAsync(app);

// =====================================================
// 6) Middlewares
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorCors");

// Se no futuro entrar JWT, app.UseAuthentication() vem antes
app.UseAuthorization();

// Controllers legados (se houver)
app.MapControllers();

// =====================================================
// 7) V5 Endpoints (Inventory V5 oficial)
// =====================================================
app.MapV5Endpoints();
app.MapV5Suppliers();
app.MapV5RestocksEndpoints();
app.MapV5OutboxEndpoints();

app.Run();


// =====================================================
// Helper: cria DB + containers shows V5
// =====================================================
static async Task EnsureCosmosSchemaAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var opt = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.DatabaseId))
        throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

    var dbResp = await cosmos.CreateDatabaseIfNotExistsAsync(opt.DatabaseId);
    var db = dbResp.Database;

    // ✅ Containers V5 (schema híbrido /pk)
    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Products, "/pk"));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Suppliers, "/pk"));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.ReorderRules, "/pk"));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Operations, "/pk"));

    // 🔒 Legado mantido apenas para histórico (não tocar)

    if (!env.IsDevelopment())
        return;
}
