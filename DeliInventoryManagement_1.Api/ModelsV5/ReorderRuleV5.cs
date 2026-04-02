using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class ReorderRuleV5 : CosmosDocument
{
    public ReorderRuleV5()
    {
        Type = "ReorderRule";
        Pk = "STORE#1";
    }

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [JsonPropertyName("reorderLevel")]
    public int ReorderLevel { get; set; }

    [JsonPropertyName("reorderQty")]
    public int ReorderQty { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}