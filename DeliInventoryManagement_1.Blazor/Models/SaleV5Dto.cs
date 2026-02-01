using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models;

public sealed class SaleV5Dto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Sale";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}
