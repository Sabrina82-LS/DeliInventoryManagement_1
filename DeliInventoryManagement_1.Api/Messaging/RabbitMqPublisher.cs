using System.Text;
using System.Text.Json;
using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliInventoryManagement_1.Api.Messaging;

public sealed class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _ch;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private readonly RabbitMqOptions _opt;

    // routing keys/queues principais
    private static readonly string[] KnownRoutingKeys =
    [
        "sale.created",
        "restock.created"
    ];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RabbitMqPublisher(IOptions<RabbitMqOptions> opt, ILogger<RabbitMqPublisher> logger)
    {
        _opt = opt.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.Username,
            Password = _opt.Password,
            DispatchConsumersAsync = true,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        
        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        _ch.BasicReturn += OnMessageReturned;

        // ✅ Confirms
        _ch.ConfirmSelect();

        // ✅ Ensure topology (Exchange + Queues + Retry + DLQ)
        EnsureTopology();

        _logger.LogInformation(
            "🐇 RabbitMQ Publisher connected: {Host}:{Port} exchange={Exchange} retryExchange={RetryExchange} ttlMs={TtlMs}",
            _opt.Host, _opt.Port, _opt.Exchange, _opt.RetryExchange, _opt.RetryTtlMs);
    }

    private void EnsureTopology()
    {
        // 1) Exchanges
        _ch.ExchangeDeclare(exchange: _opt.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
        _ch.ExchangeDeclare(exchange: _opt.RetryExchange, type: ExchangeType.Direct, durable: true, autoDelete: false);

        foreach (var rk in KnownRoutingKeys)
        {
            var mainQueue = rk;                 // sale.created
            var retryQueue = $"{rk}.retry";     // sale.created.retry
            var dlqQueue = $"{rk}.dlq";         // sale.created.dlq

            // 2) DLQ
            _ch.QueueDeclare(queue: dlqQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            // 3) Retry (TTL + DLX volta pro exchange principal)
            var retryArgs = new Dictionary<string, object>
            {
                ["x-message-ttl"] = _opt.RetryTtlMs,
                ["x-dead-letter-exchange"] = _opt.Exchange,
                ["x-dead-letter-routing-key"] = rk
            };

            _ch.QueueDeclare(queue: retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs);

            // 4) Main (DLX aponta para retry exchange)
            var mainArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _opt.RetryExchange,
                ["x-dead-letter-routing-key"] = retryQueue
            };

            _ch.QueueDeclare(queue: mainQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainArgs);

            // 5) Bindings
            _ch.QueueBind(queue: mainQueue, exchange: _opt.Exchange, routingKey: rk);
            _ch.QueueBind(queue: retryQueue, exchange: _opt.RetryExchange, routingKey: retryQueue);
        }

        _logger.LogInformation("✅ RabbitMQ topology ensured (main + retry + dlq) for: {Keys}",
            string.Join(", ", KnownRoutingKeys));
    }

    public void Publish(string routingKey, object payload, string messageId)
    {
        if (string.IsNullOrWhiteSpace(routingKey))
            throw new ArgumentException("routingKey is required", nameof(routingKey));

        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("messageId is required", nameof(messageId));

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _ch.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = messageId;
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _ch.BasicPublish(
            exchange: _opt.Exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body);

        _ch.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

        _logger.LogInformation(
            "✅ Published messageId={MessageId} routingKey={RoutingKey} exchange={Exchange}",
            messageId, routingKey, _opt.Exchange);
    }

    private void OnMessageReturned(object? sender, BasicReturnEventArgs e)
    {
        var msgId = e.BasicProperties?.MessageId ?? "(no messageId)";
        _logger.LogError(
            "⚠️ RabbitMQ RETURNED messageId={MessageId} replyCode={ReplyCode} replyText={ReplyText} exchange={Exchange} routingKey={RoutingKey}",
            msgId, e.ReplyCode, e.ReplyText, e.Exchange, e.RoutingKey);
    }

    public void Dispose()
    {
        try { _ch.BasicReturn -= OnMessageReturned; } catch { }
        try { _ch?.Close(); } catch { }
        try { _conn?.Close(); } catch { }

        _ch?.Dispose();
        _conn?.Dispose();
    }
}
