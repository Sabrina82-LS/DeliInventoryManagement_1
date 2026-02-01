using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class ReorderRuleV5 : CosmosDocument
{
    public ReorderRuleV5()
    {
        Type = "ReorderRule";
    }

    // regra por produto
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("reorderLevel")]
    public int ReorderLevel { get; set; } = 5;

    [JsonPropertyName("reorderQty")]
    public int ReorderQty { get; set; } = 5;

    // ex: SupplierId preferencial
    [JsonPropertyName("preferredSupplierId")]
    public string? PreferredSupplierId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
