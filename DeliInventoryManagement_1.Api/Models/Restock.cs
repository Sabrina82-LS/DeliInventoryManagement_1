using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Restock
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Type { get; set; } = "Restock";

    public DateTime Date { get; set; }
    public int Quantity { get; set; }

    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public List<RestockLine> Lines { get; set; } = new();

    public decimal TotalCost { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
