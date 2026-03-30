using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;

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

        using var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    public async Task<List<ProductV5Dto>> GetAllProductsAsync(string? search = null, string? categoryId = null)
    {
        var products = await GetAsync<List<ProductV5Dto>>("/api/v5/products")
                       ?? new List<ProductV5Dto>();

        IEnumerable<ProductV5Dto> query = products;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();
            query = query.Where(p => (p.Name ?? string.Empty)
                .Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            var category = categoryId.Trim();
            query = query.Where(p => string.Equals(p.CategoryId, category, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    public async Task<List<CategoryV5Dto>> GetAllCategoriesAsync()
    {
        var products = await GetAllProductsAsync();

        return products
            .Where(p => !string.IsNullOrWhiteSpace(p.CategoryId))
            .GroupBy(p => p.CategoryId!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryV5Dto
            {
                Id = group.Key,
                Name = group.Select(x => x.CategoryName)
                            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? group.Key
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<List<SaleV5Dto>> GetAllSalesAsync()
    {
        return await GetAsync<List<SaleV5Dto>>("/api/v5/sales")
               ?? new List<SaleV5Dto>();
    }

    public async Task<List<ReorderRuleV5Dto>> GetAllReorderRulesAsync()
    {
        return await GetAsync<List<ReorderRuleV5Dto>>("/api/v5/reorderrules")
               ?? new List<ReorderRuleV5Dto>();
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

    public async Task<List<ReorderListItemV5Dto>> GetLowStockTop10Async()
    {
        return await GetAsync<List<ReorderListItemV5Dto>>("/api/v5/reorder/low-stock-top10")
               ?? new List<ReorderListItemV5Dto>();
    }

    public async Task<List<ReorderListItemV5Dto>> GetPendingLowStockTop10Async()
    {
        return await GetAsync<List<ReorderListItemV5Dto>>("/api/v5/reorder/pending-low-stock-top10")
               ?? new List<ReorderListItemV5Dto>();
    }

    public async Task<int> GetSupplierCountAsync()
    {
        var suppliers = await GetAsync<List<SupplierV5Dto>>("/api/v5/suppliers")
                        ?? new List<SupplierV5Dto>();

        return suppliers.Count;
    }
}