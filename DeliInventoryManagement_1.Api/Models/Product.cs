using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Product
{
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = string.Empty;


    [JsonPropertyName(nameof(Type))]
    public string Type { get; set; } = nameof(Product);

    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName(nameof(CategoryId))]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName(nameof(CategoryName))]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName(nameof(Quantity))]
    public int Quantity { get; set; }

    [JsonPropertyName(nameof(Cost))]
    public decimal Cost { get; set; }

    [JsonPropertyName(nameof(Price))]
    public decimal Price { get; set; }

    [JsonPropertyName(nameof(ReorderLevel))]
    public int ReorderLevel { get; set; } = 5;

}
