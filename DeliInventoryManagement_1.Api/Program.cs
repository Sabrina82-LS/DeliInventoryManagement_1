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
// 1) BASIC INFRASTRUCTURE
// Registers controllers, memory cache, and Swagger UI
// so we can explore and test the API via /swagger
// =====================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSwaggerGen(c =>
{
    // Basic API info shown in the Swagger UI header
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DeliInventoryManagement API",
        Version = "v1"
    });

    // Adds the "Authorize" button in Swagger so we can
    // paste our JWT token and test protected endpoints
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Type: Bearer {your JWT token}"
    });

    // Makes all endpoints require the Bearer token by default
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

// =====================================================
// 2) JSON SERIALIZATION OPTIONS
// Uses camelCase for all JSON responses (e.g. productId)
// and accepts both camelCase and PascalCase in requests
// =====================================================
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// 3) CORS — Cross Origin Resource Sharing
// Allows the Blazor frontend to call this API.
// Origins are loaded from appsettings/environment vars
// so we can add the deployed Blazor URL without
// changing code.
// In Azure set: Cors__AllowedOrigins__0 = https://your-blazor.azurewebsites.net
// =====================================================
const string CorsPolicyName = "BlazorCors";

// Reads allowed origins from config (appsettings or Azure env vars)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// =====================================================
// 4) AUTHENTICATION AND AUTHORIZATION
// Uses JWT Bearer tokens. The token is created by
// JwtTokenService after a successful login.
// Two roles are supported: Admin and Staff.
// - AdminOnly: only Admin users can access
// - AdminOrStaff: both Admin and Staff can access
// =====================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

// Fail fast if JWT key is missing — app cannot run securely without it
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Must match Jwt:Issuer
            ValidateAudience = true,         // Must match Jwt:Audience
            ValidateLifetime = true,         // Token must not be expired
            ValidateIssuerSigningKey = true, // Must be signed with our key
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Only users with Role = "Admin" can access AdminOnly endpoints
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // Users with Role = "Admin" or "Staff" can access these endpoints
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff"));
});

// Service that creates JWT tokens after login
builder.Services.AddSingleton<JwtTokenService>();

// =====================================================
// 5) COSMOS DB
// Reads connection settings from CosmosDb section.
// Creates a single shared CosmosClient (singleton)
// which is the recommended pattern for performance.
// CosmosContainerFactory provides typed access to
// each container (Products, Operations, etc.)
// =====================================================
builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddSingleton(sp =>
{
    var opt = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    // Fail fast if Cosmos connection info is missing
    if (string.IsNullOrWhiteSpace(opt.AccountEndpoint) ||
        string.IsNullOrWhiteSpace(opt.AccountKey))
    {
        throw new InvalidOperationException(
            "CosmosDb: AccountEndpoint and AccountKey must be configured.");
    }

    // Use System.Text.Json serializer (faster than Newtonsoft)
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

// Factory that provides easy access to each Cosmos container
builder.Services.AddSingleton<CosmosContainerFactory>();

// =====================================================
// 6) RABBITMQ MESSAGING
// RabbitMQ is used for event-driven messaging between
// services (e.g. SaleCreated, RestockCreated events).
// The publisher and service are always registered so
// they can be injected anywhere.
// IMPORTANT: The hosted services that actually connect
// to RabbitMQ only start in Development. In Production
// (Azure) they are skipped so the app does not crash
// if RabbitMQ is not available.
// =====================================================
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

// Publisher sends messages to RabbitMQ exchanges
builder.Services.AddSingleton<RabbitMqPublisher>();

// Service abstraction used by SalesService etc.
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Only start RabbitMQ consumers locally in Development
// In Production, the Outbox pattern handles reliability instead
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<RabbitMqHostedService>();
    builder.Services.AddHostedService<SaleCreatedConsumer>();
    builder.Services.AddHostedService<RestockCreatedConsumer>();
    builder.Services.AddHostedService<OutboxDispatcherV5>();
}

// =====================================================
// 7) DOMAIN SERVICES
// Business logic services for the main entities.
// Scoped = one instance per HTTP request.
// =====================================================
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

// Build the application
var app = builder.Build();

// =====================================================
// 8) STARTUP BOOTSTRAP
// On first run this creates all Cosmos containers if
// they don't exist, and seeds the default Admin and
// Staff users so we can log in straight away.
// Skipped in Testing environment to keep tests fast.
// =====================================================
if (!app.Environment.IsEnvironment("Testing"))
{
    await EnsureCosmosSchemaAsync(app);
    await EnsureSeedUsersAsync(app);
    await SeedRunnerV5.RunAsync(app.Services, app.Environment);
}

// =====================================================
// 9) MIDDLEWARE PIPELINE
// Order matters here — CORS must come before Auth,
// Auth must come before Authorization.
// Swagger is enabled in all environments so we can
// test the deployed API on Azure via /swagger
// =====================================================

// Serve the swagger.json spec file
app.UseSwagger();

// Serve the Swagger UI at /swagger
app.UseSwaggerUI();

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Apply CORS policy — must be before UseAuthentication
app.UseCors(CorsPolicyName);

// Validate JWT tokens on incoming requests
app.UseAuthentication();

// Check role policies on protected endpoints
app.UseAuthorization();

// Map MVC controller routes
app.MapControllers();

// =====================================================
// 10) API ENDPOINTS
// All minimal API endpoints are registered here.
// Each MapXxx() method is defined in its own file
// under the Endpoints folder.
// =====================================================

// Products and Sales (V5Endpoints.cs)
app.MapV5Endpoints();

// Suppliers CRUD
app.MapV5Suppliers();

// Restock operations (add stock from supplier)
app.MapV5RestocksEndpoints();

// Outbox event monitoring (pending/published/failed)
app.MapV5OutboxEndpoints();

// Sales and Restock reports
app.MapReportsV5();

// Login endpoint
app.MapAuthV5();

// User management (Admin only)
app.MapUsersV5();

app.Run();

// =====================================================
// HELPER METHODS
// =====================================================

// Creates all Cosmos DB containers if they don't exist.
// Safe to run on every startup — uses CreateIfNotExists.
static async Task EnsureCosmosSchemaAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var opt = scope.ServiceProvider
        .GetRequiredService<IOptions<CosmosOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.DatabaseId))
        throw new InvalidOperationException(
            "CosmosDb: DatabaseId is not configured.");

    // Create database if it doesn't exist
    var db = (await cosmos.CreateDatabaseIfNotExistsAsync(opt.DatabaseId)).Database;

    // Create each container with the configured partition key (/pk)
    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Products, opt.PartitionKeyPath));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Suppliers, opt.PartitionKeyPath));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.ReorderRules, opt.PartitionKeyPath));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Operations, opt.PartitionKeyPath));

    await db.CreateContainerIfNotExistsAsync(
        new ContainerProperties(opt.Containers.Users, opt.PartitionKeyPath));
}

// Seeds the default Admin and Staff users on first run.
// UserSeed checks if users already exist before creating them
// so this is safe to run on every startup.
static async Task EnsureSeedUsersAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await UserSeed.EnsureUsersAsync(cosmos, config);
}

// Needed for integration testing — allows the test project
// to reference this Program class
public partial class Program { }