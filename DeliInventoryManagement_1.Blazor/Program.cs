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
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("Missing configuration: Api:BaseUrl in appsettings.json");

// =============================
// 3) HttpClient para a API
// =============================
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<ISalesService, SalesService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<IRestockService, RestockService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

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
