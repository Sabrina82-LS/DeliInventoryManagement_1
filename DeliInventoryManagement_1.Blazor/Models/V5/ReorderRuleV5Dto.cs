using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models.V5;

public class ReorderRuleV5Dto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("minStockLevel")]
    public int MinStockLevel { get; set; }

    [JsonPropertyName("reorderQuantity")]
    public int ReorderQuantity { get; set; }
}