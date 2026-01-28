using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Product
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;


    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Product";

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("CategoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("CategoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("Cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("Price")]
    public decimal Price { get; set; }

    [JsonPropertyName("ReorderLevel")]
    public int ReorderLevel { get; set; } = 5;

}
