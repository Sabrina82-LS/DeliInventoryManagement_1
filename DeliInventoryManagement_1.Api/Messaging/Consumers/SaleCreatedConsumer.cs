using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Messaging.Consumers;

public sealed class SaleCreatedConsumer : RabbitConsumerBase
{
    private readonly ILogger<SaleCreatedConsumer> _logger;

    public SaleCreatedConsumer(IOptions<RabbitMqOptions> opt, ILogger<SaleCreatedConsumer> logger)
        : base(opt, logger)
    {
        _logger = logger;
    }

    protected override string QueueName => "sale.created";
    protected override string RoutingKey => "sale.created";

    // opcional: mudar limite aqui se quiser
    protected override int MaxRetries => 5;

    protected override Task HandleAsync(string messageId, string body, CancellationToken ct)
    {
        _logger.LogInformation("📩 SALE CONSUMED messageId={MessageId} body={Body}", messageId, body);

        // ✅ opcional: simular erro para testar retry/dlq
        if (body.Contains("\"quantity\":999", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Simulated failure (quantity=999)");

        return Task.CompletedTask;
    }
}
