using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public class DashboardService : IDashboardService
{
    private readonly HttpClient _http;

    public DashboardService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProductSummaryDto> GetProductSummaryAsync()
    {
        var summary = await _http.GetFromJsonAsync<ProductSummaryDto>(
            "api/v1/products/summary");

        return summary ?? new ProductSummaryDto();
    }

    public async Task<int> GetSupplierCountAsync()
    {
        var suppliers = await _http.GetFromJsonAsync<List<SupplierDto>>(
            "api/v3/suppliers");

        return suppliers?.Count ?? 0;
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(int top)
    {
        var result = await _http.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"api/v1/products?sortBy=quantity&sortDir=asc&page=1&pageSize={top}");

        var items = result?.Items ?? new List<ProductDto>();

        return items.Where(p => p.ReorderLevel <= 0 || p.Quantity < p.ReorderLevel)
                    .ToList();
    }

    public async Task<List<ProductDto>> GetAllProductsAsync(string? search, string? category)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category))
            query.Add($"categoryId={Uri.EscapeDataString(category)}");

        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;

        var result = await _http.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"api/v1/products{qs}");

        return result?.Items ?? new List<ProductDto>();
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var items = await _http.GetFromJsonAsync<List<CategoryDto>>("api/v2/categories");
        return items ?? new List<CategoryDto>();
    }

    public async Task<List<SupplierDto>> GetAllSuppliersAsync()
    {
        var items = await _http.GetFromJsonAsync<List<SupplierDto>>("api/v3/suppliers");
        return items ?? new List<SupplierDto>();
    }

}
