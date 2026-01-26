using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Supplier
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;


    [JsonProperty("Type")]
    public string Type { get; set; } = "Supplier";

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("ContactName")]
    public string? ContactName { get; set; }

    [JsonProperty("Phone")]
    public string? Phone { get; set; }

    [JsonProperty("Email")]
    public string? Email { get; set; }

    [JsonProperty("Address")]
    public string? Address { get; set; }

    [JsonProperty("Notes")]
    public string? Notes { get; set; }
}
