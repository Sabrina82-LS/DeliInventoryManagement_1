using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.IService;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly HttpClient _httpClient;

    public PurchaseOrderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReorderOrderV5Dto>> GetOrdersAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ReorderOrderV5Dto>>("api/v5/reorder/orders")
               ?? new List<ReorderOrderV5Dto>();
    }

    public async Task<ReorderOrderV5Dto?> GetOrderByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<ReorderOrderV5Dto>($"api/v5/reorder/orders/{id}");
    }

    public async Task<bool> MarkAsCompletedAsync(string id)
    {
        var response = await _httpClient.PutAsync($"api/v5/reorder/orders/{id}/complete", null);
        return response.IsSuccessStatusCode;
    }
}