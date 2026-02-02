using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;

namespace DeliInventoryManagement_1.Blazor.Services;

public class SuppliersServiceV5 : ISuppliersServiceV5
{
    private readonly HttpClient _http;

    public SuppliersServiceV5(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<SupplierV5Dto>> GetAllAsync()
    {
        var res = await _http.GetFromJsonAsync<List<SupplierV5Dto>>("api/v5/suppliers");
        return res ?? new List<SupplierV5Dto>();
    }

    public async Task<SupplierV5Dto?> GetByIdAsync(string id)
    {
        return await _http.GetFromJsonAsync<SupplierV5Dto>($"api/v5/suppliers/{id}");
    }

    public async Task<SupplierV5Dto> CreateAsync(SupplierV5Dto dto)
    {
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
        var resp = await _http.DeleteAsync($"api/v5/suppliers/{id}");
        resp.EnsureSuccessStatusCode();
    }
}
