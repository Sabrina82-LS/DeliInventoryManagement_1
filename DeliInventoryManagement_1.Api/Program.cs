// =====================================================
// PROGRAM.CS — Application Entry Point
// This file bootstraps the entire API:
//   1. Registers all services into the DI container
//   2. Builds the app
//   3. Configures the middleware pipeline
//   4. Maps all API endpoints
//   5. Runs the application
// =====================================================

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

// Creates the WebApplication builder which gives us access
// to configuration (appsettings.json, env vars) and services (DI container)
var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1) BASIC INFRASTRUCTURE
// Registers controllers, memory cache, and Swagger UI
// so we can explore and test the API via /swagger
// =====================================================

// Enables minimal API endpoint discovery for Swagger
builder.Services.AddEndpointsApiExplorer();

// Registers MVC controllers (needed for any [ApiController] classes)
builder.Services.AddControllers();

// Registers in-memory cache — used to cache frequently read data
// like product lists to reduce CosmosDB calls
builder.Services.AddMemoryCache();

// Configures Swagger/OpenAPI documentation
// This generates the interactive API docs available at /swagger
builder.Services.AddSwaggerGen(options =>
{
    // Basic API info shown in the Swagger UI header
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DeliInventoryManagement API",
        Version = "v1"
    });

    // Adds the "Authorize" button in Swagger so we can
    // paste our JWT token and test protected endpoints
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",        // HTTP header name
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",             // lowercase required by spec
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    // Makes all endpoints in Swagger show the lock icon
    // and require a Bearer token to be tested
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
            Array.Empty<string>() // No specific scopes required
        }
    });
});

// =====================================================
// 2) JSON SERIALIZATION OPTIONS
// Uses camelCase for all JSON responses (e.g. productId)
// and accepts both camelCase and PascalCase in requests
// =====================================================
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Responses will use camelCase: { "productId": 1 } not { "ProductId": 1 }
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

    // Requests can use either camelCase or PascalCase — more flexible for clients
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// =====================================================
// 3) CORS — Cross Origin Resource Sharing
// Allows the Blazor frontend to call this API.
// Origins are loaded from appsettings/environment vars
// so we can add the deployed Blazor URL without
// changing code.
// In Azure set: Cors__AllowedOrigins__0 = https://your-blazor.azurewebsites.net
// =====================================================

// Name for our CORS policy — used later in middleware pipeline
const string CorsPolicyName = "BlazorCors";

// Reads allowed origins from appsettings.json or Azure environment variables
// Example appsettings.json:
// "Cors": { "AllowedOrigins": [ "https://localhost:7001" ] }
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        // Only allow requests from our known frontend origins
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()  // Allow Content-Type, Authorization, etc.
              .AllowAnyMethod(); // Allow GET, POST, PUT, DELETE, etc.
    });
});

// =====================================================
// 4) AUTHENTICATION AND AUTHORIZATION
// Uses JWT Bearer tokens. The token is created by
// JwtTokenService after a successful login.
// Two roles are supported: Admin and Staff.
// - AdminOnly: only Admin users can access
// - AdminOrStaff: both Admin and Staff can access
// =====================================================

// Read JWT settings from appsettings.json under the "Jwt" section
// Example: { "Jwt": { "Key": "...", "Issuer": "...", "Audience": "..." } }
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

// Fail fast — if JWT key is missing the app cannot create
// or validate tokens so there is no point starting up
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json.");
}

// Register JWT Bearer authentication
// Every request with an Authorization: Bearer <token> header
// will be validated against these parameters
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Token must come from our issuer
            ValidateAudience = true,         // Token must be intended for our audience
            ValidateLifetime = true,         // Token must not be expired
            ValidateIssuerSigningKey = true, // Token must be signed with our secret key
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            // Convert the key string to bytes for cryptographic validation
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Define authorization policies based on user roles
// These are used with [Authorize(Policy = "AdminOnly")] on endpoints
builder.Services.AddAuthorization(options =>
{
    // Only users with Role = "Admin" can access AdminOnly endpoints
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // Users with Role = "Admin" or "Staff" can access AdminOrStaff endpoints
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff"));
});

// Service that generates JWT tokens after successful login
// Singleton because it is stateless and safe to share across requests
builder.Services.AddSingleton<JwtTokenService>();

// =====================================================
// 5) COSMOS DB
// Reads connection settings from CosmosDb section.
// Creates a single shared CosmosClient (singleton)
// which is the recommended pattern for performance.
// CosmosContainerFactory provides typed access to
// each container (Products, Suppliers, Operations, etc.)
// =====================================================

// Binds the "CosmosDb" section of appsettings.json to CosmosOptions class
// So we can inject IOptions<CosmosOptions> anywhere we need Cosmos settings
builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection("CosmosDb"));

// Register CosmosClient as a singleton — Microsoft recommends one instance
// per application lifetime for best performance and connection reuse
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;

    // Fail fast if connection info is missing — app cannot talk to database
    if (string.IsNullOrWhiteSpace(options.AccountEndpoint) ||
        string.IsNullOrWhiteSpace(options.AccountKey))
    {
        throw new InvalidOperationException(
            "CosmosDb:AccountEndpoint and CosmosDb:AccountKey must be configured.");
    }

    // Use System.Text.Json for serialization (faster than Newtonsoft)
    // with case-insensitive property matching for flexibility
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    // Create and return the CosmosClient with our custom serializer
    return new CosmosClient(
        options.AccountEndpoint,
        options.AccountKey,
        new CosmosClientOptions
        {
            Serializer = new CosmosStjSerializer(serializerOptions)
        });
});

// Factory that provides easy typed access to each Cosmos container
// e.g. _factory.GetContainer(ContainerName.Products)
builder.Services.AddSingleton<CosmosContainerFactory>();

// =====================================================
// 6) RABBITMQ MESSAGING
// RabbitMQ is used for event-driven messaging between
// services (e.g. SaleCreated, RestockCreated events).
// IMPORTANT: Consumers only start in Development.
// In Production (Azure) they are skipped so the app
// does not crash if RabbitMQ is not available.
// The Outbox pattern handles reliability in Production.
// =====================================================

// Binds "RabbitMQ" section of appsettings.json to RabbitMqOptions
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

// Publisher sends messages to RabbitMQ exchanges
// Singleton because it manages a long-lived connection
builder.Services.AddSingleton<RabbitMqPublisher>();

// Service abstraction used by SalesService, RestockService etc.
// Using an interface allows easy mocking in unit tests
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Only register RabbitMQ consumers in Development
// In Production we use the Outbox pattern instead for reliability
if (builder.Environment.IsDevelopment())
{
    // Hosted service that manages the RabbitMQ connection lifecycle
    builder.Services.AddHostedService<RabbitMqHostedService>();

    // Listens for SaleCreated events and processes them asynchronously
    builder.Services.AddHostedService<SaleCreatedConsumer>();

    // Listens for RestockCreated events and processes them asynchronously
    builder.Services.AddHostedService<RestockCreatedConsumer>();

    // Processes outbox messages and dispatches them to RabbitMQ
    builder.Services.AddHostedService<OutboxDispatcherV5>();
}

// =====================================================
// 7) DOMAIN SERVICES
// Business logic services for the main entities.
// Scoped = one instance per HTTP request, disposed after.
// This is the standard lifetime for services that use
// database connections or per-request state.
// =====================================================
builder.Services.AddScoped<ISalesService, SalesService>();       // Sales business logic
builder.Services.AddScoped<IProductService, ProductService>();   // Product business logic
builder.Services.AddScoped<ISupplierService, SupplierService>(); // Supplier business logic

// =====================================================
// BUILD THE APPLICATION
// After this line no more services can be registered.
// The DI container is compiled and locked.
// =====================================================
var app = builder.Build();

// =====================================================
// 8) STARTUP BOOTSTRAP
// On first run this creates all Cosmos containers if
// they don't exist, and seeds the default Admin and
// Staff users so we can log in straight away.
// Skipped in Testing environment to keep tests fast
// and avoid needing a real CosmosDB connection.
// =====================================================
if (!app.Environment.IsEnvironment("Testing"))
{
    // Create CosmosDB containers if they don't exist yet
    await EnsureCosmosSchemaAsync(app);

    // Seed default Admin and Staff users on first run
    await EnsureSeedUsersAsync(app);

    // Seed all V5 reference data (products, suppliers, reorder rules)
    await SeedRunnerV5.RunAsync(app.Services, app.Environment);
}

// =====================================================
// 9) MIDDLEWARE PIPELINE
// The ORDER here is critical — each middleware wraps
// the next one like layers of an onion.
// CORS must come before Auth so preflight requests
// are handled before token validation runs.
// Auth must come before Authorization so the user
// identity is established before role checks happen.
// =====================================================

// Generates and serves the swagger.json spec at /swagger/v1/swagger.json
app.UseSwagger();

// Serves the Swagger UI web page at /swagger
// Allows interactive testing of all endpoints in the browser
app.UseSwaggerUI();

// Redirects all HTTP requests to HTTPS for security
app.UseHttpsRedirection();

// Apply CORS policy — must be before UseAuthentication
// so browser preflight OPTIONS requests are handled correctly
app.UseCors(CorsPolicyName);

// Reads and validates the JWT token from the Authorization header
// Sets HttpContext.User if the token is valid
app.UseAuthentication();

// Checks the user's role against endpoint policies
// e.g. endpoints marked with [Authorize(Policy = "AdminOnly")]
app.UseAuthorization();

// Maps MVC controller routes (any [ApiController] classes)
app.MapControllers();

// =====================================================
// 10) API ENDPOINTS
// All minimal API endpoints are registered here.
// Each MapXxx() method is an extension method defined
// in its own file under the Endpoints folder.
// This keeps Program.cs clean and focused.
// =====================================================

// V5 Products and Sales endpoints
app.MapV5Endpoints();

// Suppliers CRUD endpoints
app.MapV5Suppliers();

// Restock operations (add stock from supplier)
app.MapV5RestocksEndpoints();

// Outbox event monitoring (view pending/published/failed messages)
app.MapV5OutboxEndpoints();

// Sales and Restock report endpoints
app.MapReportsV5();

// Reorder workflow endpoints
app.MapV5Reorder();

// Reorder rules management endpoints
app.MapV5ReorderRules();

// Login and token generation endpoint
app.MapAuthV5();

// User management endpoints (Admin only)
app.MapUsersV5();

// Start the web server and begin listening for requests
app.Run();

// =====================================================
// HELPER METHODS
// Defined as static local functions so they can be
// called above but keep the top of the file clean.
// =====================================================

/// <summary>
/// Creates all required CosmosDB containers if they don't exist.
/// Safe to run on every startup — uses CreateIfNotExistsAsync
/// which does nothing if the container already exists.
/// </summary>
static async Task EnsureCosmosSchemaAsync(WebApplication app)
{
    // Create a DI scope to resolve services safely
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

    // Fail fast if database name is missing from config
    if (string.IsNullOrWhiteSpace(options.DatabaseId))
    {
        throw new InvalidOperationException("CosmosDb:DatabaseId is not configured.");
    }

    // Create the database if it doesn't exist yet
    var database = (await cosmos.CreateDatabaseIfNotExistsAsync(options.DatabaseId)).Database;

    // Create each container with partition key path — idempotent, safe every startup
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

/// <summary>
/// Seeds the default Admin and Staff users on first run.
/// UserSeed checks if users already exist before creating them
/// so this is completely safe to run on every startup.
/// </summary>
static async Task EnsureSeedUsersAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // UserSeed handles the "already exists" check internally
    await UserSeed.EnsureUsersAsync(cosmos, config);
}

// Needed for integration testing — exposes the Program class
// so the test project can reference it with WebApplicationFactory<Program>
public partial class Program { }