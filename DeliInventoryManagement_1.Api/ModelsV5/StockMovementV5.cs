using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class StockMovementV5 : CosmosDocument
{
    public StockMovementV5()
    {
        Type = "StockMovement";
    }

    // Ex: "SALE", "RESTOCK", "ADJUSTMENT"
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    // Ex: SALE#..., RESTOCK#...
    [JsonPropertyName("refId")]
    public string RefId { get; set; } = string.Empty;

    [JsonPropertyName("lines")]
    public List<StockMovementLineV5> Lines { get; set; } = new();
}

public sealed class StockMovementLineV5
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    // delta negativo para venda, positivo para reposição
    [JsonPropertyName("delta")]
    public int Delta { get; set; }
}
