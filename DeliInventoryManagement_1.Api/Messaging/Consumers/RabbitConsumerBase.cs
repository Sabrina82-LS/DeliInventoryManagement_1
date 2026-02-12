using System.Text;
using DeliInventoryManagement_1.Api.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliInventoryManagement_1.Api.Messaging.Consumers;

public abstract class RabbitConsumerBase : BackgroundService
{
    private readonly RabbitMqOptions _opt;
    private readonly ILogger _logger;

    private IConnection? _conn;
    private IModel? _ch;
    private string _consumerTag = "";

    protected RabbitConsumerBase(IOptions<RabbitMqOptions> opt, ILogger logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    // cada consumer implementa como tratar a mensagem
    protected abstract Task HandleAsync(string messageId, string body, CancellationToken ct);

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

        // garante exchange + queue + bind (idempotente)
        _ch.ExchangeDeclare(_opt.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        _ch.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _ch.QueueBind(
            queue: QueueName,
            exchange: _opt.Exchange,
            routingKey: RoutingKey);

        // QoS para não pegar um monte de msgs de uma vez
        _ch.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_ch);

        consumer.Received += async (_, ea) =>
        {
            var msgId = ea.BasicProperties?.MessageId ?? "(no-messageId)";
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                await HandleAsync(msgId, body, stoppingToken);

                // ✅ sucesso -> ACK
                _ch.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                _logger.LogInformation("✅ ACK {Queue} messageId={MessageId}", QueueName, msgId);
            }
            catch (Exception ex)
            {
                // ✅ falhou -> NACK e requeue (por enquanto)
                _logger.LogError(ex, "❌ NACK {Queue} messageId={MessageId}", QueueName, msgId);

                _ch.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _consumerTag = _ch.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("🐇 Consumer started: queue={Queue} rk={RoutingKey}", QueueName, RoutingKey);

        // background service não pode terminar, então segura aqui
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_ch is not null && !string.IsNullOrWhiteSpace(_consumerTag))
                _ch.BasicCancel(_consumerTag);
        }
        catch { }

        try { _ch?.Close(); } catch { }
        try { _conn?.Close(); } catch { }

        _ch?.Dispose();
        _conn?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
