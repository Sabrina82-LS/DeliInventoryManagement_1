using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliInventoryManagement_1.Blazor.Models.CreateRequest;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;

namespace DeliInventoryManagement_1.Blazor.Services.Service.cs;

public class RestockService : IRestockService
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    public RestockService(HttpClient http, AuthState authState)
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

    public async Task CreateAsync(CreateRestockRequest request)
    {
        ApplyBearerToken();

        var res = await _http.PostAsJsonAsync("api/v5/restocks", request);
        res.EnsureSuccessStatusCode();
    }
}