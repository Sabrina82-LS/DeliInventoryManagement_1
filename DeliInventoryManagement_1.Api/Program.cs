using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Endpoints;
using DeliInventoryManagement_1.Api.Endpoints.V5;
using DeliInventoryManagement_1.Api.Messaging;
using DeliInventoryManagement_1.Api.Messaging.Consumers;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Services.Outbox;
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
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// ✅ JSON (camelCase) para Minimal APIs
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// 2) CORS (Blazor -> API)
// =====================================================
const string CorsPolicyName = "BlazorCors";

var allowedOrigins = new[]
{
    "https://localhost:7081",
    "http://localhost:7081"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// =====================================================
// 3) Authorization
// =====================================================
builder.Services.AddAuthorization();

// =====================================================
// 4) Cosmos Options + CosmosClient + Factory
// =====================================================
builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.AccountEndpoint) || string.IsNullOrWhiteSpace(opt.AccountKey))
        throw new InvalidOperationException("CosmosDb: configure AccountEndpoint e AccountKey no appsettings.json.");

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
// 5) RabbitMQ Options + Producer
// =====================================================
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMqPublisher>();

// =====================================================
// 6) Consumers (11.4/11.5)
// =====================================================
builder.Services.AddHostedService<SaleCreatedConsumer>();
builder.Services.AddHostedService<RestockCreatedConsumer>();

// =====================================================
// 7) Outbox Dispatcher (10.2 + 11.3)
// =====================================================
builder.Services.AddHostedService<OutboxDispatcherV5>();

var app = builder.Build();

// =====================================================
// 8) Startup: garante DB + Containers V5
// =====================================================
await EnsureCosmosSchemaAsync(app);

// =====================================================
// 9) Middlewares
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseAuthorization();

app.MapControllers();

// =====================================================
// 10) Endpoints V5
// =====================================================
app.MapV5Endpoints();
app.MapV5Suppliers();
app.MapV5RestocksEndpoints();
app.MapV5OutboxEndpoints();
app.MapReportsV5();

app.Run();

// =====================================================
// Helper: cria DB + containers V5 (/pk)
// =====================================================
static async Task EnsureCosmosSchemaAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var opt = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.DatabaseId))
        throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

    var db = (await cosmos.CreateDatabaseIfNotExistsAsync(opt.DatabaseId)).Database;

    // Containers V5 (Partition Key: /pk)
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Products, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Suppliers, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.ReorderRules, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Operations, "/pk"));
}
