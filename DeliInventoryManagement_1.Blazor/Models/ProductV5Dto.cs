using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Blazor.Models;

public sealed class ProductV5Dto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("pk")]
    public string Pk { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Product";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = "";

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("reorderLevel")]
    public int ReorderLevel { get; set; } = 5;

    [JsonPropertyName("reorderQty")]
    public int ReorderQty { get; set; } = 0;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
