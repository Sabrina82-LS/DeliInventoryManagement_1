using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Category
{
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = default!;


    [JsonProperty(nameof(Type))]
    public string Type { get; set; } = nameof(Category);

    [JsonProperty(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(nameof(Description))]
    public string? Description { get; set; }
}
