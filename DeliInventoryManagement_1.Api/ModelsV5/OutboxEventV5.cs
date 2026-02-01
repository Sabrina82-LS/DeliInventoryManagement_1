using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class OutboxEventV5 : CosmosDocument
{
    public OutboxEventV5()
    {
        Type = "OutboxEvent";
    }

    // "Pending", "Published", "Failed"
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    // ex: "SaleCreated"
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("attempts")]
    public int Attempts { get; set; } = 0;

    // Próxima tentativa (retry)
    [JsonPropertyName("nextAttemptAtUtc")]
    public DateTime? NextAttemptAtUtc { get; set; }

    // Mensagem de erro se falhar
    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    // Payload genérico (fica flexível p/ professor)
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new { };
}
