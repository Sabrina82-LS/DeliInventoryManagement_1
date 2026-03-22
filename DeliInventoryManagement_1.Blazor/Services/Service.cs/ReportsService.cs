using DeliInventoryManagement_1.Blazor.Models.Report;
using DeliInventoryManagement_1.Blazor.Services.Auth;
using DeliInventoryManagement_1.Blazor.Services.IService;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class ReportsService : IReportsService
{
    private readonly HttpClient _http;
    private readonly AuthState _authState;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportsService(HttpClient http, AuthState authState)
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

    private async Task<T?> GetAsync<T>(string url)
    {
        ApplyBearerToken();

        using var resp = await _http.GetAsync(url);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return default;

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private static string BuildDateQuery(DateTime? from, DateTime? to)
    {
        var parts = new List<string>();

        if (from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd"))}");

        if (to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd"))}");

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    public async Task<List<SaleReportItem>> GetSalesAsync(DateTime? from, DateTime? to)
    {
        var qs = BuildDateQuery(from, to);
        return await GetAsync<List<SaleReportItem>>($"/api/v5/reports/sales{qs}") ?? new();
    }

    public async Task<List<RestockReportItem>> GetRestocksAsync(DateTime? from, DateTime? to)
    {
        var qs = BuildDateQuery(from, to);
        return await GetAsync<List<RestockReportItem>>($"/api/v5/reports/restocks{qs}") ?? new();
    }
}