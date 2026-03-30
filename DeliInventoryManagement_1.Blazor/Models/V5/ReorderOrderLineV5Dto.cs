using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models.V5;

public sealed class ReorderOrderLineV5Dto
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantityRequested")]
    public int QuantityRequested { get; set; }

    [JsonPropertyName("unitCost")]
    public decimal UnitCost { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }
}