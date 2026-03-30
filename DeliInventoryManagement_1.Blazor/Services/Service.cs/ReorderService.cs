using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.IService;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class ReorderService : IReorderService
{
    private readonly HttpClient _httpClient;

    public ReorderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReorderListItemV5Dto>> GetReorderListAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ReorderListItemV5Dto>>("api/v5/reorder/list")
               ?? new List<ReorderListItemV5Dto>();
    }

    public async Task<List<ReorderListItemV5Dto>> GetLowStockTop10Async()
    {
        return await _httpClient.GetFromJsonAsync<List<ReorderListItemV5Dto>>("api/v5/reorder/low-stock-top10")
               ?? new List<ReorderListItemV5Dto>();
    }

    public async Task<bool> ConfirmReorderAsync(ConfirmReorderRequestV5Dto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v5/reorder/confirm", request);
        return response.IsSuccessStatusCode;
    }
}