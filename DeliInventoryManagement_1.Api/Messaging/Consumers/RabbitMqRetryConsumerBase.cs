using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliInventoryManagement_1.Api.Messaging;

public abstract class RabbitMqRetryConsumerBase<T> : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IConnection _conn;
    private readonly IModel _ch;

    private readonly string _queue;     // ex: sale.created
    private readonly string _dlqQueue;  // ex: sale.created.dlq
    private readonly int _maxRetries;

    protected RabbitMqRetryConsumerBase(
        IConfiguration cfg,
        ILogger logger,
        string queue,
        int maxRetries = 5)
    {
        _logger = logger;
        _queue = queue;
        _dlqQueue = $"{queue}.dlq";
        _maxRetries = maxRetries;

        var r = cfg.GetSection("RabbitMQ");

        var host = r["Host"] ?? "localhost";
        var port = int.TryParse(r["Port"], out var p) ? p : 5672;
        var user = r["Username"] ?? "admin";
        var pass = r["Password"] ?? "admin123";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            DispatchConsumersAsync = true,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        // evita “pegar tudo de uma vez”
        _ch.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_ch);

        consumer.Received += async (_, ea) =>
        {
            if (stoppingToken.IsCancellationRequested) return;

            var deliveryTag = ea.DeliveryTag;

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var data = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data is null)
                    throw new InvalidOperationException("Message payload is null/invalid JSON");

                await HandleAsync(data, ea.BasicProperties, stoppingToken);

                _ch.BasicAck(deliveryTag, multiple: false);

                _logger.LogInformation("✅ ACK queue={Queue} messageId={MessageId}",
                    _queue, ea.BasicProperties?.MessageId);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCountFromXDeath(ea.BasicProperties?.Headers, _queue);

                _logger.LogWarning(ex,
                    "❌ FAIL queue={Queue} messageId={MessageId} retryCount={RetryCount}/{MaxRetries}",
                    _queue, ea.BasicProperties?.MessageId, retryCount, _maxRetries);

                if (retryCount >= _maxRetries)
                {
                    // manda pra DLQ e remove original (ACK)
                    PublishToDlq(ea);
                    _ch.BasicAck(deliveryTag, multiple: false);

                    _logger.LogError("🧨 Sent to DLQ queue={DlqQueue} messageId={MessageId}",
                        _dlqQueue, ea.BasicProperties?.MessageId);
                }
                else
                {
                    // manda pro retry via DLX do main queue
                    _ch.BasicNack(deliveryTag, multiple: false, requeue: false);
                }
            }
        };

        _ch.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);

        _logger.LogInformation("🎧 Consumer started: queue={Queue} dlq={DlqQueue} maxRetries={MaxRetries}",
            _queue, _dlqQueue, _maxRetries);

        return Task.CompletedTask;
    }

    // ✅ seu processamento real fica aqui (cada consumer implementa)
    protected abstract Task HandleAsync(T message, IBasicProperties? props, CancellationToken ct);

    private void PublishToDlq(BasicDeliverEventArgs ea)
    {
        // publica direto na fila DLQ usando default exchange ""
        var props = _ch.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = ea.BasicProperties?.ContentType ?? "application/json";
        props.MessageId = ea.BasicProperties?.MessageId;
        props.Headers = ea.BasicProperties?.Headers; // mantém x-death e outros headers

        _ch.BasicPublish(
            exchange: "",
            routingKey: _dlqQueue,
            basicProperties: props,
            body: ea.Body);
    }

    private static int GetRetryCountFromXDeath(IDictionary<string, object>? headers, string queueName)
    {
        if (headers is null) return 0;
        if (!headers.TryGetValue("x-death", out var xDeathObj)) return 0;

        // x-death é uma lista de tabelas (List<object>) com dicts dentro
        if (xDeathObj is not IList<object> deaths) return 0;

        // soma counts onde a death foi nessa queue
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
                // count pode vir como long/int
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
