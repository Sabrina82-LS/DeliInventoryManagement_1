using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class SupplierV5 : CosmosDocument
{
    public SupplierV5()
    {
        Type = "Supplier";
        Pk = "STORE#1";
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}