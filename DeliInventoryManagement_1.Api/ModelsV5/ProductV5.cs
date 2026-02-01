using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class ProductV5 : CosmosDocument
{
    public ProductV5()
    {
        Type = "Product";
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    // ✅ estoque único (simplificado) — depois, se quiser, evolui para Batches/Lots
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("reorderLevel")]
    public int ReorderLevel { get; set; } = 5;

    [JsonPropertyName("reorderQty")]
    public int ReorderQty { get; set; } = 5;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
