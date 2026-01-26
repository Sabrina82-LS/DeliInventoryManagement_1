using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Sales
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Sale";

    [JsonPropertyName("ProductId")]
    public string ProductId { get; set; } = default!;

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; } = default!;

    [JsonPropertyName("CategoryId")]
    public string CategoryId { get; set; } = default!;

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("UnitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("Total")]
    public decimal Total { get; set; }

    [JsonPropertyName("CreatedAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}
