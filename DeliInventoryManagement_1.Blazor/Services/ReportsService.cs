using System.Net;
using System.Text.Json;
using DeliInventoryManagement_1.Blazor.Models.Report;

namespace DeliInventoryManagement_1.Blazor.Services;

public sealed class ReportsService : IReportsService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportsService(HttpClient http)
    {
        _http = http;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        using var resp = await _http.GetAsync(url);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return default;

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private static string BuildDateQuery(DateTime? from, DateTime? to)
    {
        // Monta ?from=YYYY-MM-DD&to=YYYY-MM-DD (sem pk!)
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
