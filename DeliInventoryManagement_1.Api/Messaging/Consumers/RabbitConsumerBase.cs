using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DeliInventoryManagement_1.Api.Messaging.Consumers;

public abstract class RabbitConsumerBase : BackgroundService
{
    private readonly RabbitMqOptions _opt;
    private readonly ILogger _logger;

    private IConnection? _conn;
    private IModel? _ch;

    protected RabbitConsumerBase(IOptions<RabbitMqOptions> opt, ILogger logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected virtual int MaxRetries => 5;
    protected virtual ushort PrefetchCount => 10;
    protected virtual string DlqQueueName => $"{QueueName}.dlq";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        _ch.BasicQos(0, PrefetchCount, global: false);

        // Garante DLQ
        _ch.QueueDeclare(queue: DlqQueueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_ch);

        consumer.Received += async (_, ea) =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            if (_ch is null)
                return;

            var messageId = ea.BasicProperties?.MessageId ?? "(no messageId)";
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                await HandleAsync(messageId, body, stoppingToken);

                _ch.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "✅ ACK queue={Queue} messageId={MessageId}",
                    QueueName, messageId);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCountFromXDeath(ea.BasicProperties?.Headers, QueueName);

                _logger.LogWarning(ex,
                    "❌ FAIL queue={Queue} messageId={MessageId} retry={Retry}/{Max}",
                    QueueName, messageId, retryCount, MaxRetries);

                if (retryCount >= MaxRetries)
                {
                    PublishToDlq(_ch, ea);

                    _ch.BasicAck(ea.DeliveryTag, multiple: false);

                    _logger.LogError(
                        "🧨 MOVED TO DLQ queue={Dlq} messageId={MessageId}",
                        DlqQueueName, messageId);
                }
                else
                {
                    // Envia para retry via DLX
                    _ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
        };

        _ch.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation(
            "🎧 Consumer started queue={Queue} dlq={Dlq} maxRetries={MaxRetries}",
            QueueName, DlqQueueName, MaxRetries);

        return Task.CompletedTask;
    }

    protected abstract Task HandleAsync(string messageId, string body, CancellationToken ct);

    private void PublishToDlq(IModel ch, BasicDeliverEventArgs ea)
    {
        var props = ch.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = ea.BasicProperties?.ContentType ?? "application/json";
        props.MessageId = ea.BasicProperties?.MessageId;
        props.Headers = ea.BasicProperties?.Headers;

        ch.BasicPublish(
            exchange: "",
            routingKey: DlqQueueName,
            basicProperties: props,
            body: ea.Body);
    }

    private static int GetRetryCountFromXDeath(IDictionary<string, object>? headers, string queueName)
    {
        if (headers is null) return 0;
        if (!headers.TryGetValue("x-death", out var xDeathObj)) return 0;
        if (xDeathObj is not IList<object> deaths) return 0;

        var total = 0;

        foreach (var d in deaths)
        {
            if (d is not IDictionary<string, object> death) continue;

            if (!death.TryGetValue("queue", out var qObj)) continue;
            var q = qObj?.ToString();

            if (!string.Equals(q, queueName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (death.TryGetValue("count", out var cObj))
            {
                if (cObj is long l) total += (int)l;
                else if (cObj is int i) total += i;
                else if (int.TryParse(cObj?.ToString(), out var parsed)) total += parsed;
            }
        }

        return total;
    }

    public override void Dispose()
    {
        try { _ch?.Close(); } catch { }
        try { _conn?.Close(); } catch { }

        _ch?.Dispose();
        _conn?.Dispose();

        base.Dispose();
    }
}
