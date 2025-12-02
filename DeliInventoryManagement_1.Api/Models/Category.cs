using Newtonsoft.Json;

namespace DeliInventoryManagement_1.Api.Models;

public class Category
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("Type")]
    public string Type { get; set; } = "Category";

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("Description")]
    public string? Description { get; set; }
}
