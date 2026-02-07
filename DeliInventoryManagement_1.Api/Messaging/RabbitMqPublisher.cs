using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliInventoryManagement_1.Api.Messaging;

public sealed class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _ch;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly string _exchange;

    // ✅ routing keys/queues que seu projeto usa
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

    public RabbitMqPublisher(IConfiguration cfg, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var r = cfg.GetSection("RabbitMQ");

        var host = r["Host"] ?? "localhost";
        var port = int.TryParse(r["Port"], out var p) ? p : 5672;
        var user = r["Username"] ?? "admin";
        var pass = r["Password"] ?? "admin123";

        _exchange = r["Exchange"] ?? "inventory.events";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            DispatchConsumersAsync = true,

            // ✅ recovery
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        // ✅ se mensagem não for roteada para nenhuma queue, RabbitMQ devolve (return)
        _ch.BasicReturn += OnMessageReturned;

        // 1) Exchange (idempotente)
        _ch.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);

        // 2) Declara queues + binds (idempotente)
        foreach (var rk in KnownRoutingKeys)
        {
            // queue name == routing key (simples e consistente)
            _ch.QueueDeclare(queue: rk, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _ch.QueueBind(queue: rk, exchange: _exchange, routingKey: rk);
        }

        // 3) Confirms (boa prática)
        _ch.ConfirmSelect();

        _logger.LogInformation(
            "🐇 RabbitMQ Publisher connected: {Host}:{Port} exchange={Exchange} bindings=[{Bindings}]",
            host, port, _exchange, string.Join(", ", KnownRoutingKeys));
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

        // ✅ mandatory=true: se não houver fila/binding, dispara BasicReturn (você vê no log)
        _ch.BasicPublish(
            exchange: _exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body);

        // ✅ confirma entrega ao broker
        _ch.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

        _logger.LogInformation(
            "✅ Published messageId={MessageId} routingKey={RoutingKey} exchange={Exchange}",
            messageId, routingKey, _exchange);
    }

    private void OnMessageReturned(object? sender, BasicReturnEventArgs e)
    {
        // Isso acontece quando a mensagem NÃO foi roteada para nenhuma fila (sem binding)
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
