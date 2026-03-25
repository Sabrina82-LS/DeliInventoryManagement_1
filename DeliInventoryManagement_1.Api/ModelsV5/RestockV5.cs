using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class RestockV5 : CosmosDocument
{
    public RestockV5()
    {
        Type = "Restock";
        Pk = "STORE#1";
    }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [JsonPropertyName("lines")]
    public List<RestockLineV5> Lines { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}