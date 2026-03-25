using DeliInventoryManagement_1.Api.ModelsV5.Line;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class SaleV5 : CosmosDocument
{
    public SaleV5()
    {
        Type = "Sale";
        Pk = "STORE#1";
    }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lines")]
    public List<SaleLineV5> Lines { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}