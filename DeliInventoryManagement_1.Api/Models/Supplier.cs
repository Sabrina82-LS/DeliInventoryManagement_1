using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Models;

public class Supplier
{
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = default!;


    [JsonProperty(nameof(Type))]
    public string Type { get; set; } = nameof(Supplier);

    [JsonProperty(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(nameof(ContactName))]
    public string? ContactName { get; set; }

    [JsonProperty(nameof(Phone))]
    public string? Phone { get; set; }

    [JsonProperty(nameof(Email))]
    public string? Email { get; set; }

    [JsonProperty(nameof(Address))]
    public string? Address { get; set; }

    [JsonProperty(nameof(Notes))]
    public string? Notes { get; set; }
}
