using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class SaleV5 : CosmosDocument
{
    public SaleV5()
    {
        Type = "Sale";
    }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lines")]
    public List<SaleLineV5> Lines { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

public sealed class SaleLineV5
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
}
