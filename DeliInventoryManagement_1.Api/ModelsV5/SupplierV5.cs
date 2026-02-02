using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models.V5;

public class SupplierV5
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    // Partition Key do container Suppliers ("/pk")
    [JsonPropertyName("pk")]
    public string Pk { get; set; } = "supplier";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Supplier";

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
