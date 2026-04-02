using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models.V5;

public sealed class ReorderOrderV5Dto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("lines")]
    public List<ReorderOrderLineV5Dto> Lines { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}