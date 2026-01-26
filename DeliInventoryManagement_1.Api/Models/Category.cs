using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;


    [JsonProperty("Type")]
    public string Type { get; set; } = "Category";

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("Description")]
    public string? Description { get; set; }
}
