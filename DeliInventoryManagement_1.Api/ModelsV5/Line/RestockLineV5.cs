using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class RestockLineV5
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitCost")]
    public decimal UnitCost { get; set; }

    // Calculated property (not stored in Cosmos)
    [JsonIgnore]
    public decimal TotalCost => Quantity * UnitCost;
}