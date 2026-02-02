using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.ModelsV5;

public sealed class OutboxEventV5 : CosmosDocument
{
    public OutboxEventV5()
    {
        Type = "OutboxEvent";
    }

    // Ex: "RestockCreated" / "SaleCreated"
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    // Ex: restockId / saleId
    [JsonPropertyName("aggregateId")]
    public string AggregateId { get; set; } = string.Empty;

    // Ex: "RESTOCK" / "SALE"
    [JsonPropertyName("aggregateType")]
    public string AggregateType { get; set; } = string.Empty;

    // Json do evento (payload para RabbitMQ)
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new { };

    // Pending | Processing | Published | Failed
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("attempts")]
    public int Attempts { get; set; } = 0;

    // ✅ Usa CreatedAtUtc do CosmosDocument (não duplicar aqui)
    // CosmosDocument.CreatedAtUtc já existe

    // Ajuda para auditoria / dispatcher
    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("publishedAtUtc")]
    public DateTime? PublishedAtUtc { get; set; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    // (Opcional, mas excelente) trava temporária para evitar 2 processadores pegarem o mesmo evento
    [JsonPropertyName("lockedUntilUtc")]
    public DateTime? LockedUntilUtc { get; set; }
}
