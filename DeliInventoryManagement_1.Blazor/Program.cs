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

builder.Services.AddHttpContextAccessor();

// ======================================================
// 2) Read API Base URL from configuration
//    (appsettings.json -> Api:BaseUrl)
// ======================================================
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]?.Trim();

if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("Missing configuration: Api:BaseUrl in appsettings.json.");

if (!apiBaseUrl.EndsWith("/"))
    apiBaseUrl += "/";

var apiUri = new Uri(apiBaseUrl, UriKind.Absolute);

// ======================================================
// 3) Browser Storage
// ======================================================
builder.Services.AddScoped<ProtectedLocalStorage>();

// ======================================================
// 4) Authentication State + Auth Services
// ======================================================
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<AuthService>();

// ======================================================
// 5) Named HttpClients
// ======================================================

// Public client (login / no token)
builder.Services.AddHttpClient("ApiNoAuth", client =>
{
    client.BaseAddress = apiUri;
});

// Protected/default client
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = apiUri;
});

// ======================================================
// 6) Typed Services
// ======================================================
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

// Fallback HttpClient -> uses named client "Api"
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

// ======================================================
// 7) Build application
// ======================================================
var app = builder.Build();

// ======================================================
// 8) Middleware Pipeline
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
// 9) Map Razor Components
// ======================================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();