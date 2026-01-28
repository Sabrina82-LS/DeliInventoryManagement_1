using DeliInventoryManagement_1.Blazor.Components;
using DeliInventoryManagement_1.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1) Razor Components (Server)
// =============================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =============================
// 2) API Base URL (centralizado)
// =============================
const string ApiBaseUrl = "https://localhost:7022/";
// Se seu Swagger/API estiver em outra porta (ex: 7202), troque aqui.

// =============================
// 3) HttpClient para a API
// =============================

// 3.1) Client nomeado "Api" (recomendado para reutilizar em vários services)
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// 3.2) Services que consomem a API (tipados)
// ✅ DashboardService (GET products/categories/suppliers/summary etc.)
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// ✅ SalesService (POST /api/v4/sales)
builder.Services.AddHttpClient<ISalesService, SalesService>(client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// =====================================================
// ✅ PASSO 4: RestockService (POST /api/restocks)
// - Este service será usado pela página Restocks.razor
// - Envia o restock para a API e a API aumenta o stock no Cosmos
// =====================================================
builder.Services.AddHttpClient<IRestockService, RestockService>(client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// 3.3) (Opcional) HttpClient padrão apontando pra API
//      Útil se você quiser injetar HttpClient diretamente em algum component/service
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
