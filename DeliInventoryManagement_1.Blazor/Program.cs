using DeliInventoryManagement_1.Blazor.Components;
using DeliInventoryManagement_1.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Razor Components (Server)
// =============================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =============================
// API Base URL (centralizado)
// =============================
const string ApiBaseUrl = "https://localhost:7022/";
// Se seu Swagger/API estiver em outra porta (ex: 7202), troque aqui.

// =============================
// HttpClient para a API
// =============================

// 1) Client nomeado "Api" (recomendado para reutilizar em vários services)
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// 2) Services que consomem a API
builder.Services.AddHttpClient<IDashboardService, DashboardService>(client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// ✅ PASSO 3: SalesService (POST /api/v4/sales)
builder.Services.AddHttpClient<ISalesService, SalesService>(client =>
{
    client.BaseAddress = new Uri(ApiBaseUrl);
});

// 3) (Opcional) HttpClient padrão apontando pra API
//    Útil se você quiser injetar HttpClient diretamente em algum component/service
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api")
);

var app = builder.Build();

// =============================
// Pipeline
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
