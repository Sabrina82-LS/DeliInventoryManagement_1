using Newtonsoft.Json;

namespace DeliInventoryManagement_1.Api.Models;

public class Product
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("Type")]
    public string Type { get; set; } = "Product";

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("CategoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonProperty("CategoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonProperty("Quantity")]
    public int Quantity { get; set; }

    [JsonProperty("Cost")]
    public decimal Cost { get; set; }

    [JsonProperty("Price")]
    public decimal Price { get; set; }
}
