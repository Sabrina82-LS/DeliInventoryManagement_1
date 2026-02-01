using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Endpoints;
using DeliInventoryManagement_1.Api.Services;
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

// Se você ainda tem controllers (ex: SalesController.cs) pode manter.
// Se não usa, pode remover AddControllers + MapControllers.
builder.Services.AddControllers();

builder.Services.AddMemoryCache();

// ✅ JSON para Minimal APIs (camalCase no swagger/body)
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
// 3) Authorization (FIX do erro "AddAuthorization")
// =====================================================
builder.Services.AddAuthorization();

// =====================================================
// 4) Cosmos Options + CosmosClient + ContainerFactory
// =====================================================
builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.AccountEndpoint) || string.IsNullOrWhiteSpace(opt.AccountKey))
        throw new InvalidOperationException("CosmosDb: configure AccountEndpoint e AccountKey no appsettings.json.");

    var stjOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    return new CosmosClient(opt.AccountEndpoint, opt.AccountKey, new CosmosClientOptions
    {
        // mantém o seu serializer
        Serializer = new CosmosStjSerializer(stjOptions)
    });
});

builder.Services.AddSingleton<CosmosContainerFactory>();

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

// Se você não usa autenticação, tudo bem manter só Authorization.
// (Se um dia for usar JWT, aí entra app.UseAuthentication() antes)
app.UseAuthorization();

// Se não tiver controllers, pode apagar as duas linhas abaixo.
app.MapControllers();

// =====================================================
// 7) V5 Endpoints (novo oficial)
// =====================================================
app.MapV5Endpoints();

app.Run();


// =====================================================
// Helper: cria DB + containers do schema V5
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

    // ✅ Containers V5 (schema /pk)
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Products, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Suppliers, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.ReorderRules, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Operations, "/pk"));

    // 🔥 Legado: NÃO criar/apagar aqui. Mantém somente para histórico.
    // Seeds: se quiser depois, fazemos SeedRunnerV5 bem limpo.

    if (!env.IsDevelopment())
        return;
}
