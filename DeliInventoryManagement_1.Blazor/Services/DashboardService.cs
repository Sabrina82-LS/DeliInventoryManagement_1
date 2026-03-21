using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.Auth;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardService(HttpClient http, AuthState authState)
    {
        _http = http;
        _authState = authState;
    }

    private void ApplyBearerToken()
    {
        if (!string.IsNullOrWhiteSpace(_authState.Token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authState.Token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        ApplyBearerToken();

        using var resp = await _http.GetAsync(url);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return default;

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    public async Task<List<ProductV5Dto>> GetAllProductsAsync(string? search = null, string? categoryId = null)
    {
        var products = await GetAsync<List<ProductV5Dto>>("/api/v5/products") ?? new List<ProductV5Dto>();

        IEnumerable<ProductV5Dto> q = products;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => (p.Name ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            var c = categoryId.Trim();
            q = q.Where(p => string.Equals(p.CategoryId, c, StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }

    public async Task<List<CategoryV5Dto>> GetAllCategoriesAsync()
    {
        var products = await GetAllProductsAsync();

        return products
            .Where(p => !string.IsNullOrWhiteSpace(p.CategoryId))
            .GroupBy(p => p.CategoryId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategoryV5Dto
            {
                Id = g.Key,
                Name = g.Select(x => x.CategoryName)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<List<SaleV5Dto>> GetAllSalesAsync()
    {
        return await GetAsync<List<SaleV5Dto>>("/api/v5/sales") ?? new List<SaleV5Dto>();
    }

    public async Task<ProductSummaryDto> GetProductSummaryAsync()
    {
        var products = await GetAllProductsAsync();

        return new ProductSummaryDto
        {
            Count = products.Count,
            TotalQuantity = products.Sum(p => p.Quantity)
        };
    }

    public async Task<List<ProductV5Dto>> GetLowStockProductsAsync(int top = 5)
    {
        var products = await GetAllProductsAsync();

        return products
            .Where(p => p.Quantity <= p.ReorderLevel)
            .OrderBy(p => p.Quantity)
            .Take(top)
            .ToList();
    }

    public Task<int> GetSupplierCountAsync()
    {
        return Task.FromResult(0);
    }
}