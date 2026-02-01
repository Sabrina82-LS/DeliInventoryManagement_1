using System.Net;
using System.Text.Json;
using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public class DashboardService : IDashboardService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardService(HttpClient http)
    {
        _http = http;
    }

    // -------------------------
    // Helper HTTP
    // -------------------------
    private async Task<T?> GetAsync<T>(string url)
    {
        using var resp = await _http.GetAsync(url);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return default;

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // -------------------------
    // V5 (direto)
    // -------------------------
    public async Task<List<ProductV5Dto>> GetV5ProductsAsync()
        => await GetAsync<List<ProductV5Dto>>("api/v5/products") ?? new();

    public async Task<List<SaleV5Dto>> GetV5SalesAsync()
        => await GetAsync<List<SaleV5Dto>>("api/v5/sales") ?? new();

    // -------------------------
    // Compatibilidade (páginas antigas)
    // -------------------------
    public async Task<List<ProductDto>> GetAllProductsAsync(string? search = null, string? categoryId = null)
    {
        var v5 = await GetV5ProductsAsync();

        IEnumerable<ProductV5Dto> q = v5;

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => (p.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(categoryId))
            q = q.Where(p => string.Equals(p.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));

        return q.Select(p => new ProductDto
        {
            Id = p.Id ?? "",
            Name = p.Name ?? "",
            CategoryId = p.CategoryId ?? "",
            CategoryName = p.CategoryName ?? "",
            Quantity = p.Quantity,
            Price = p.Price,
            Cost = p.Cost,
            ReorderLevel = p.ReorderLevel
        }).ToList();
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var v5 = await GetV5ProductsAsync();

        // Deriva categories dos produtos
        return v5
            .Where(p => !string.IsNullOrWhiteSpace(p.CategoryId))
            .GroupBy(p => p.CategoryId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategoryDto
            {
                Id = g.Key,
                Name = g.Select(x => x.CategoryName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<List<SupplierDto>> GetAllSuppliersAsync()
    {
        // Se você ainda não implementou /api/v5/suppliers, retorna vazio (não quebra o app).
        var v5 = await GetAsync<List<SupplierDto>>("api/v5/suppliers");
        return v5 ?? new List<SupplierDto>();
    }

    // -------------------------
    // Dashboard (summary/low stock)
    // -------------------------
    public async Task<ProductSummaryDto> GetProductSummaryAsync()
    {
        var products = await GetAllProductsAsync();

        return new ProductSummaryDto
        {
            Count = products.Count,
            TotalQuantity = products.Sum(p => p.Quantity)
        };
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(int top = 5)
    {
        var products = await GetAllProductsAsync();

        return products
            .Where(p => p.Quantity <= p.ReorderLevel)
            .OrderBy(p => p.Quantity)
            .Take(top)
            .ToList();
    }

    public async Task<int> GetSupplierCountAsync()
    {
        var suppliers = await GetAllSuppliersAsync();
        return suppliers.Count;
    }
}
