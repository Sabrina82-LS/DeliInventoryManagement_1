using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Data.Seed;
using DeliInventoryManagement_1.Api.Endpoints;
using DeliInventoryManagement_1.Api.Endpoints.V5;
using DeliInventoryManagement_1.Api.Messaging;
using DeliInventoryManagement_1.Api.Messaging.Consumers;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Services.Auth;
using DeliInventoryManagement_1.Api.Services.Outbox;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) Basic infrastructure + Swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Swagger + Authorize button (Bearer JWT)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DeliInventoryManagement API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Type: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JSON for Minimal APIs
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
// 3) Authentication (JWT) + Authorization (Roles)
// =====================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key não configurada no appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff"));
});

// JWT service
builder.Services.AddSingleton<JwtTokenService>();

// =====================================================
// 4) Cosmos options + CosmosClient + Factory
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
// 5) RabbitMQ options + producer
// =====================================================
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddHostedService<RabbitMqHostedService>();

// =====================================================
// 6) Domain services
// =====================================================
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

// =====================================================
// 7) Consumers (11.4 / 11.5)
// =====================================================
builder.Services.AddHostedService<SaleCreatedConsumer>();
builder.Services.AddHostedService<RestockCreatedConsumer>();

// =====================================================
// 8) Outbox dispatcher (10.2 + 11.3)
// =====================================================
builder.Services.AddHostedService<OutboxDispatcherV5>();

var app = builder.Build();

// =====================================================
// 9) Startup bootstrap
//    Skip Cosmos initialization and user seeding in Testing
// =====================================================
if (!app.Environment.IsEnvironment("Testing"))
{
    await EnsureCosmosSchemaAsync(app);
    await EnsureSeedUsersAsync(app);
}

// =====================================================
// 10) Middlewares
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

// Correct middleware order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================================================
// 11) Endpoints V5
// =====================================================
app.MapV5Endpoints();
app.MapV5Suppliers();
app.MapV5RestocksEndpoints();
app.MapV5OutboxEndpoints();
app.MapReportsV5();

app.MapAuthV5();
app.MapUsersV5();

app.Run();

// =====================================================
// Helpers
// =====================================================
static async Task EnsureCosmosSchemaAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var opt = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.DatabaseId))
        throw new InvalidOperationException("CosmosDb: DatabaseId não configurado.");

    var db = (await cosmos.CreateDatabaseIfNotExistsAsync(opt.DatabaseId)).Database;

    // V5 containers (Partition Key: /pk)
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Products, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Suppliers, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.ReorderRules, "/pk"));
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties(opt.Containers.Operations, "/pk"));

    // Users container
    await db.CreateContainerIfNotExistsAsync(new ContainerProperties("Users", "/pk"));
}

static async Task EnsureSeedUsersAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await UserSeed.EnsureUsersAsync(cosmos, config);
}

public partial class Program { }