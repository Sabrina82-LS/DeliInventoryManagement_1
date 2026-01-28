using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models;

namespace DeliInventoryManagement_1.Blazor.Services;

public class SalesService : ISalesService
{
    private readonly HttpClient _http;

    public SalesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/v4/sales", req);

        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"API error {(int)resp.StatusCode}: {body}");

        return (await resp.Content.ReadFromJsonAsync<CreateSaleResponse>())!;
    }
}
