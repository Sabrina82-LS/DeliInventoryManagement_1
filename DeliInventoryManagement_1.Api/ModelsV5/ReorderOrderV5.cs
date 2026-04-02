using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public class ReorderOrderV5
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = "STORE#1";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "ReorderOrder";

    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = default!;

    [JsonPropertyName("supplierName")]
    public string SupplierName { get; set; } = default!;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("lines")]
    public List<ReorderOrderLineV5> Lines { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}