using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public abstract class CosmosDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // ✅ Partition Key do modelo híbrido
    [JsonPropertyName("pk")]
    public string Pk { get; set; } = "STORE#1";

    // ⚠️ NÃO usar construtor — evita conflito com System.Type
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
