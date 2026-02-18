using DeliInventoryManagement_1.Blazor.Components;
using DeliInventoryManagement_1.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1) Razor Components (Server)
// =============================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =============================
// 2) API Base URL (appsettings)
// =============================
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]?.Trim();

if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("Missing configuration: Api:BaseUrl in appsettings.json");

// Normaliza: garante que termina com "/"
if (!apiBaseUrl.EndsWith("/"))
    apiBaseUrl += "/";

var apiUri = new Uri(apiBaseUrl, UriKind.Absolute);

// =============================
// 3) HttpClient base para a API
// =============================

// Named client "Api" (centraliza BaseAddress)
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = apiUri;
});

// Services tipados usando o mesmo BaseAddress
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
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

// ✅ NOVO: Reports (Sales Report + Restocks Report)
builder.Services.AddHttpClient<IReportsService, ReportsService>(client =>
{
    client.BaseAddress = apiUri;
});

// ✅ HttpClient "default" para qualquer lugar que injete HttpClient diretamente
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api")
);

var app = builder.Build();

// =============================
// 4) Pipeline
// =============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
