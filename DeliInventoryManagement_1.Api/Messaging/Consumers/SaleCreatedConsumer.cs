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

    protected override Task HandleAsync(string messageId, string body, CancellationToken ct)
    {
        // Por enquanto: só logar.
        // No próximo passo você liga isso com "update stock" / "reorder rules" etc.
        _logger.LogInformation("📩 SALE CONSUMED messageId={MessageId} body={Body}", messageId, body);
        return Task.CompletedTask;
    }
}
