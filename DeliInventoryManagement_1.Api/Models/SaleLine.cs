using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class SaleLine
{
    [JsonPropertyName("ProductId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("UnitPrice")]
    public decimal UnitPrice { get; set; }
}
