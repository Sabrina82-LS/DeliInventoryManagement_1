using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Sales
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Sale";

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("CreatedAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("Lines")]
    public List<SaleLine> Lines { get; set; } = new();

    [JsonPropertyName("Total")]
    public decimal Total { get; set; }
}
