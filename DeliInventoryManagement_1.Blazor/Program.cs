using DeliInventoryManagement_1.Blazor.Components;
using DeliInventoryManagement_1.Blazor.Services;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;
using DeliInventoryManagement_1.Blazor.Services.Service.cs;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1) Configure Razor Components (Blazor Server interactive mode)
// ======================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Allows services/components to access HttpContext when needed
builder.Services.AddHttpContextAccessor();

// ======================================================
// 2) Read API base URL from configuration
//    Expected key: Api:BaseUrl
// ======================================================
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]?.Trim();

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "Missing configuration: Api:BaseUrl in appsettings.json.");
}

// Ensure the base URL ends with a slash
if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl += "/";
}

var apiUri = new Uri(apiBaseUrl, UriKind.Absolute);

// ======================================================
// 3) Register browser storage services
// ======================================================
// ProtectedLocalStorage is used to persist token/session data in the browser
builder.Services.AddScoped<ProtectedLocalStorage>();

// ======================================================
// 4) Register authentication state and auth helpers
// ======================================================
// AuthState keeps the in-memory session state
// AuthService handles login, logout, and restoring session from storage
// JwtAuthHandler appends the JWT token to outgoing protected API requests
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<JwtAuthHandler>();

// ======================================================
// 5) Register named HttpClient instances
// ======================================================

// Public client: used for endpoints that do not require authentication
// Example: login endpoint
builder.Services.AddHttpClient("ApiNoAuth", client =>
{
    client.BaseAddress = apiUri;
});

// Protected client: used for authenticated API calls
// The JWT handler automatically adds the Bearer token when available
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// ======================================================
// 6) Register typed application services
// ======================================================

// Dashboard service
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Products service
builder.Services.AddHttpClient<IProductsService, ProductsService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Sales service
builder.Services.AddHttpClient<ISalesService, SalesService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Restock service
builder.Services.AddHttpClient<IRestockService, RestockService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Suppliers service
builder.Services.AddHttpClient<ISuppliersServiceV5, SuppliersServiceV5>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Reports service
builder.Services.AddHttpClient<IReportsService, ReportsService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Reorder service
builder.Services.AddHttpClient<IReorderService, ReorderService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// Purchase order service
builder.Services.AddHttpClient<IPurchaseOrderService, PurchaseOrderService>(client =>
{
    client.BaseAddress = apiUri;
})
.AddHttpMessageHandler<JwtAuthHandler>();

// ======================================================
// 7) Register fallback HttpClient
// ======================================================
// If a component/service requests HttpClient directly,
// this fallback resolves to the protected "Api" client.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

// ======================================================
// 8) Build the application
// ======================================================
var app = builder.Build();

// ======================================================
// 9) Configure middleware pipeline
// ======================================================
if (!app.Environment.IsDevelopment())
{
    // Use a friendly error page in production
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // Enable HTTP Strict Transport Security (HSTS)
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ======================================================
// 10) Map Razor components
// ======================================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ======================================================
// 11) Run the application
// ======================================================
app.Run();