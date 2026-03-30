using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models.V5;

public sealed class ReorderListItemV5Dto
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("currentQuantity")]
    public int CurrentQuantity { get; set; }

    [JsonPropertyName("reorderLevel")]
    public int ReorderLevel { get; set; }

    [JsonPropertyName("suggestedQty")]
    public int SuggestedQty { get; set; }

    [JsonPropertyName("orderQty")]
    public int OrderQty { get; set; }

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;
}