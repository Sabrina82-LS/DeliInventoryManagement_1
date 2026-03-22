using DeliInventoryManagement_1.Blazor.Components;
using DeliInventoryManagement_1.Blazor.Services;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;
using DeliInventoryManagement_1.Blazor.Services.Service.cs;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1) Razor Components (Blazor Server - Interactive Mode)
// ======================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Allows services/components to access HttpContext if needed
builder.Services.AddHttpContextAccessor();


// ======================================================
// 2) Read API Base URL from configuration
//    (appsettings.json -> Api:BaseUrl)
// ======================================================
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]?.Trim();

if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("Missing configuration: Api:BaseUrl in appsettings.json");

// Ensure trailing slash
if (!apiBaseUrl.EndsWith("/"))
    apiBaseUrl += "/";

var apiUri = new Uri(apiBaseUrl, UriKind.Absolute);


// ======================================================
// 3) Browser Storage (Blazor Server)
// ======================================================
builder.Services.AddScoped<ProtectedLocalStorage>();


// ======================================================
// 4) Authentication State + Services
// ======================================================

// In-memory authentication state
builder.Services.AddScoped<AuthState>();

// Login / Logout / Session restore service
builder.Services.AddScoped<AuthService>();


// ======================================================
// 5) HttpClient configuration
// ======================================================

// ------------------------------------
// Client WITHOUT auth (login only)
// ------------------------------------
builder.Services.AddHttpClient("ApiNoAuth", client =>
{
    client.BaseAddress = apiUri;
});


// ------------------------------------
// Default API client
// Protected services now apply the JWT
// manually using AuthState
// ------------------------------------
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = apiUri;
});


// ------------------------------------
// Typed API services
// ------------------------------------
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    client.BaseAddress = apiUri;
});

builder.Services.AddHttpClient<IProductsService, ProductsService>(client =>
{
    client.BaseAddress = apiUri;
});



builder.Services.AddHttpClient<ISalesService, SalesService>(client =>
{
    client.BaseAddress = apiUri;
});

builder.Services.AddHttpClient<IRestockService, RestockService>(client =>
{
    client.BaseAddress = apiUri;
});

builder.Services.AddHttpClient<ISuppliersServiceV5, SuppliersServiceV5>(client =>
{
    client.BaseAddress = apiUri;
});

builder.Services.AddHttpClient<IReportsService, ReportsService>(client =>
{
    client.BaseAddress = apiUri;
});


// ------------------------------------------------------
// Default HttpClient fallback
// ------------------------------------------------------
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));


// ======================================================
// Build application
// ======================================================
var app = builder.Build();


// ======================================================
// Middleware Pipeline
// ======================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();


// ======================================================
// Map Razor Components (Blazor)
// ======================================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();