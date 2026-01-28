using DeliInventoryManagement_1.Blazor.Models;
using System.Net.Http.Json;

namespace DeliInventoryManagement_1.Blazor.Services;

public class RestockService : IRestockService
{
    private readonly HttpClient _http;

    public RestockService(HttpClient http)
    {
        _http = http;
    }

    // =====================================================
    // POST /api/restocks
    // - Envia o restock para a API
    // - A API salva o documento Restock e aumenta o stock no Cosmos
    // =====================================================
    public async Task CreateAsync(CreateRestockRequest request)
    {
        var res = await _http.PostAsJsonAsync("/api/restocks", request);
        res.EnsureSuccessStatusCode();
    }
}
