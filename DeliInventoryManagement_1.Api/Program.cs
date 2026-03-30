using System.Text;
using System.Text.Json;
using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Data;
using DeliInventoryManagement_1.Api.Data.Seed;
using DeliInventoryManagement_1.Api.Endpoints;
using DeliInventoryManagement_1.Api.Endpoints.V5;
using DeliInventoryManagement_1.Api.Messaging;
using DeliInventoryManagement_1.Api.Messaging.Consumers;
using DeliInventoryManagement_1.Api.Services;
using DeliInventoryManagement_1.Api.Services.Auth;
using DeliInventoryManagement_1.Api.Services.IService;
using DeliInventoryManagement_1.Api.Services.Outbox;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) Basic infrastructure + Swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DeliInventoryManagement API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// =====================================================
// 2) JSON options
// =====================================================
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// 3) CORS (Blazor -> API)
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
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// =====================================================
// 4) Authentication + Authorization
// =====================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json.");
}

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

builder.Services.AddSingleton<JwtTokenService>();

// =====================================================
// 5) Cosmos DB
// =====================================================
builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.AccountEndpoint) ||
        string.IsNullOrWhiteSpace(options.AccountKey))
    {
        throw new InvalidOperationException(
            "CosmosDb:AccountEndpoint and CosmosDb:AccountKey must be configured.");
    }

    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    return new CosmosClient(
        options.AccountEndpoint,
        options.AccountKey,
        new CosmosClientOptions
        {
            Serializer = new CosmosStjSerializer(serializerOptions)
        });
});

builder.Services.AddSingleton<CosmosContainerFactory>();

// =====================================================
// 6) RabbitMQ
// =====================================================
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddHostedService<RabbitMqHostedService>();

// =====================================================
// 7) Domain services
// =====================================================
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

// =====================================================
// 8) Consumers
// =====================================================
builder.Services.AddHostedService<SaleCreatedConsumer>();
builder.Services.AddHostedService<RestockCreatedConsumer>();

// =====================================================
// 9) Outbox
// =====================================================
builder.Services.AddHostedService<OutboxDispatcherV5>();

var app = builder.Build();

// =====================================================
// 10) Bootstrap
// =====================================================
if (!app.Environment.IsEnvironment("Testing"))
{
    await EnsureCosmosSchemaAsync(app);
    await EnsureSeedUsersAsync(app);
    await SeedRunnerV5.RunAsync(app.Services, app.Environment);
}

// =====================================================
// 11) Middleware pipeline
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================================================
// 12) API Endpoints
// =====================================================
app.MapV5Endpoints();
app.MapV5Suppliers();
app.MapV5RestocksEndpoints();
app.MapV5OutboxEndpoints();
app.MapReportsV5();
app.MapV5Reorder();
app.MapV5ReorderRules();
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
    var options = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.DatabaseId))
    {
        throw new InvalidOperationException("CosmosDb:DatabaseId is not configured.");
    }

    var database = (await cosmos.CreateDatabaseIfNotExistsAsync(options.DatabaseId)).Database;

    await database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(options.Containers.Products, options.PartitionKeyPath));

    await database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(options.Containers.Suppliers, options.PartitionKeyPath));

    await database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(options.Containers.ReorderRules, options.PartitionKeyPath));

    await database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(options.Containers.Operations, options.PartitionKeyPath));

    await database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(options.Containers.Users, options.PartitionKeyPath));
}

static async Task EnsureSeedUsersAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await UserSeed.EnsureUsersAsync(cosmos, config);
}

public partial class Program { }