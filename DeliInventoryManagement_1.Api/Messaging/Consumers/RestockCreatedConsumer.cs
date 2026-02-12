using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Messaging.Consumers;

public sealed class RestockCreatedConsumer : RabbitConsumerBase
{
    private readonly ILogger<RestockCreatedConsumer> _logger;

    public RestockCreatedConsumer(IOptions<RabbitMqOptions> opt, ILogger<RestockCreatedConsumer> logger)
        : base(opt, logger)
    {
        _logger = logger;
    }

    protected override string QueueName => "restock.created";
    protected override string RoutingKey => "restock.created";

    protected override Task HandleAsync(string messageId, string body, CancellationToken ct)
    {
        _logger.LogInformation("📩 RESTOCK CONSUMED messageId={MessageId} body={Body}", messageId, body);
        return Task.CompletedTask;
    }
}
