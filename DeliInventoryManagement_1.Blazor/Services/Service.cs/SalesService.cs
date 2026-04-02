using DeliInventoryManagement_1.Blazor.Models.CreateRequest;
using DeliInventoryManagement_1.Blazor.Models.V5;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DeliInventoryManagement_1.Blazor.Services.Service.cs;

public class SalesService : ISalesService
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    public SalesService(HttpClient http, AuthState authState)
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

    public async Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
    {
        ApplyBearerToken();

        var resp = await _http.PostAsJsonAsync("/api/v5/sales", req);

        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"API error {(int)resp.StatusCode}: {body}");

        var result = await resp.Content.ReadFromJsonAsync<CreateSaleResponse>();

        if (result is null)
            throw new Exception("API returned an empty response when creating the sale.");

        return result;
    }
}