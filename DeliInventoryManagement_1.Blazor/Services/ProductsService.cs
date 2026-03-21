using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class ProductsService : IProductsService
{
    private readonly HttpClient _http;

    public ProductsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ProductV5Dto>> GetProductsAsync()
    {
        return await _http.GetFromJsonAsync<List<ProductV5Dto>>("api/v5/products")
               ?? new List<ProductV5Dto>();
    }

    public async Task<ProductV5Dto?> GetProductByIdAsync(string id)
    {
        return await _http.GetFromJsonAsync<ProductV5Dto>($"api/v5/products/{id}");
    }

    public async Task<bool> CreateProductAsync(ProductV5Dto product)
    {
        var response = await _http.PostAsJsonAsync("api/v5/products", product);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateProductAsync(string id, ProductV5Dto product)
    {
        var response = await _http.PutAsJsonAsync($"api/v5/products/{id}", product);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/v5/products/{id}");
        return response.IsSuccessStatusCode;
    }
}