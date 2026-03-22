using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;

namespace DeliInventoryManagement_1.Blazor.Services.Service.cs;

public sealed class ProductsService : IProductsService
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    public ProductsService(HttpClient http, AuthState authState)
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

    public async Task<List<ProductV5Dto>> GetProductsAsync()
    {
        ApplyBearerToken();

        return await _http.GetFromJsonAsync<List<ProductV5Dto>>("api/v5/products")
               ?? new List<ProductV5Dto>();
    }

    public async Task<ProductV5Dto?> GetProductByIdAsync(string id)
    {
        ApplyBearerToken();
        return await _http.GetFromJsonAsync<ProductV5Dto>($"api/v5/products/{id}");
    }

    public async Task<bool> CreateProductAsync(ProductV5Dto product)
    {
        ApplyBearerToken();

        var response = await _http.PostAsJsonAsync("api/v5/products", product);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateProductAsync(string id, ProductV5Dto product)
    {
        ApplyBearerToken();

        var response = await _http.PutAsJsonAsync($"api/v5/products/{id}", product);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        ApplyBearerToken();

        var response = await _http.DeleteAsync($"api/v5/products/{id}");
        return response.IsSuccessStatusCode;
    }
}