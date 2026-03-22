using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;

namespace DeliInventoryManagement_1.Blazor.Services.Service.cs;

public class SuppliersServiceV5 : ISuppliersServiceV5
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    public SuppliersServiceV5(HttpClient http, AuthState authState)
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

    public async Task<List<SupplierV5Dto>> GetAllAsync()
    {
        ApplyBearerToken();

        var res = await _http.GetFromJsonAsync<List<SupplierV5Dto>>("api/v5/suppliers");
        return res ?? new List<SupplierV5Dto>();
    }

    public async Task<SupplierV5Dto?> GetByIdAsync(string id)
    {
        ApplyBearerToken();
        return await _http.GetFromJsonAsync<SupplierV5Dto>($"api/v5/suppliers/{id}");
    }

    public async Task<SupplierV5Dto> CreateAsync(SupplierV5Dto dto)
    {
        ApplyBearerToken();

        var resp = await _http.PostAsJsonAsync("api/v5/suppliers", new
        {
            name = dto.Name,
            email = dto.Email,
            phone = dto.Phone,
            notes = dto.Notes
        });

        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SupplierV5Dto>())!;
    }

    public async Task<SupplierV5Dto> UpdateAsync(string id, SupplierV5Dto dto)
    {
        ApplyBearerToken();

        var resp = await _http.PutAsJsonAsync($"api/v5/suppliers/{id}", new
        {
            name = dto.Name,
            email = dto.Email,
            phone = dto.Phone,
            notes = dto.Notes
        });

        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SupplierV5Dto>())!;
    }

    public async Task DeleteAsync(string id)
    {
        ApplyBearerToken();

        var resp = await _http.DeleteAsync($"api/v5/suppliers/{id}");
        resp.EnsureSuccessStatusCode();
    }
}