using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using DeliInventoryManagement_1.Api.Configuration;
using DeliInventoryManagement_1.Api.Messaging.Consumers;
using DeliInventoryManagement_1.Api.Tests.Mocks;

namespace DeliInventoryManagement_1.Api.Tests.Consumers
{
    public class RabbitConsumerBaseTests
    {
        private readonly Mock<ILogger<TestConsumer>> _mockLogger;
        private readonly IOptions<RabbitMqOptions> _options;
        private readonly Mock<IModel> _mockChannel;
        private readonly Mock<IConnection> _mockConnection;
        private readonly TestConsumer _consumer;

        public RabbitConsumerBaseTests()
        {
            _mockLogger = new Mock<ILogger<TestConsumer>>();

            _options = Options.Create(new RabbitMqOptions
            {
                Host = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest"
            });

            // Criar mocks do RabbitMQ
            _mockChannel = new Mock<IModel>();
            _mockConnection = new Mock<IConnection>();

            // Configurar mocks
            _mockConnection.Setup(c => c.CreateModel()).Returns(_mockChannel.Object);

            // Mock da fábrica de conexão (opcional - depende da implementação)

            _consumer = new TestConsumer(_options, _mockLogger.Object);
        }

        [Fact]
        public void AoIniciar_DeveConfigurarQueueEDLQ()
        {
            // Arrange
            var channel = new Mock<IModel>();

            // Act - Inicia o consumer (chama ExecuteAsync)
            var task = _consumer.StartAsync(CancellationToken.None);

            // Assert - Verifica se os métodos do RabbitMQ foram chamados
            _mockChannel.Verify(c => c.QueueDeclare(
                It.Is<string>(q => q.Contains("test.queue")),
                true, false, false, null), Times.Once);

            _mockChannel.Verify(c => c.QueueDeclare(
                It.Is<string>(q => q.Contains("test.queue.dlq")),
                true, false, false, null), Times.Once);

            _mockChannel.Verify(c => c.BasicConsume(
                "test.queue", false, It.IsAny<IBasicConsumer>()), Times.Once);
        }

        [Fact]
        public async Task QuandoMensagemRecebida_DeveChamarHandleAsync()
        {
            // Arrange
            bool handleFoiChamado = false;
            string mensagemRecebida = null;

            _consumer.OnHandleAsync = (messageId, body) =>
            {
                handleFoiChamado = true;
                mensagemRecebida = body;
                return Task.CompletedTask;
            };

            // Simular recebimento de mensagem
            var mensagem = "Mensagem de teste";
            await _consumer.SimularRecebimentoMensagem("msg-123", mensagem);

            // Assert
            Assert.True(handleFoiChamado);
            Assert.Equal(mensagem, mensagemRecebida);

            // Verificar se deu ACK
            _mockChannel.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Once);
        }

        [Fact]
        public async Task QuandoHandleFalhar_MasNaoExcederRetry_DeveFazerNack()
        {
            // Arrange
            _consumer.OnHandleAsync = (messageId, body) =>
            {
                throw new Exception("Falha simulada");
            };

            // Simular recebimento de mensagem
            await _consumer.SimularRecebimentoMensagem("msg-123", "Mensagem com falha");

            // Assert - Deve fazer NACK com requeue = false (para retry via DLX)
            _mockChannel.Verify(c => c.BasicNack(
                It.IsAny<ulong>(), false, false), Times.Once);

            // Não deve fazer ACK
            _mockChannel.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Never);
        }

        [Fact]
        public async Task QuandoHandleFalhar_ETiverContadorDeRetry_DeveIncrementar()
        {
            // Este teste verifica a lógica de GetRetryCountFromXDeath
            // Vamos simular headers com x-death
            // Este é um teste mais avançado que precisamos implementar
        }

        [Fact]
        public async Task QuandoHandleFalhar_MaxRetriesAtingido_DeveMoverParaDLQ()
        {
            // Arrange
            int chamadas = 0;
            _consumer.OnHandleAsync = (messageId, body) =>
            {
                chamadas++;
                throw new Exception($"Falha #{chamadas}");
            };

            // Simular 6 falhas (MaxRetries = 5, então na 6ª vai para DLQ)
            for (int i = 0; i < 6; i++)
            {
                await _consumer.SimularRecebimentoMensagem($"msg-{i}", "Mensagem com falha");
            }

            // Assert - Na 6ª tentativa, deve publicar na DLQ
            _mockChannel.Verify(c => c.BasicPublish(
                "",
                "test.queue.dlq",
                false,
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Dispose_DeveFecharConexões()
        {
            // Arrange
            _consumer.StartAsync(CancellationToken.None);

            // Act
            _consumer.Dispose();

            // Assert
            _mockChannel.Verify(c => c.Close(), Times.Once);
            _mockConnection.Verify(c => c.Close(), Times.Once);
            _mockChannel.Verify(c => c.Dispose(), Times.Once);
            _mockConnection.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Logging_DeveRegistrarAcoes()
        {
            // Arrange
            bool handleFoiChamado = false;
            _consumer.OnHandleAsync = (messageId, body) =>
            {
                handleFoiChamado = true;
                return Task.CompletedTask;
            };

            // Act
            await _consumer.SimularRecebimentoMensagem("msg-123", "Mensagem de teste");

            // Assert
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("ACK")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    // Consumer de teste que herda da sua classe base
    public class TestConsumer : RabbitConsumerBase
    {
        protected override string QueueName => "test.queue";
        protected override string RoutingKey => "test.routing";

        public Func<string, string, Task> OnHandleAsync { get; set; }
            = (messageId, body) => Task.CompletedTask;

        public TestConsumer(IOptions<RabbitMqOptions> opt, ILogger<TestConsumer> logger)
            : base(opt, logger)
        {
        }

        protected override async Task HandleAsync(string messageId, string body, CancellationToken ct)
        {
            await OnHandleAsync(messageId, body);
        }

        // Método público para testes - simula recebimento de mensagem
        public async Task SimularRecebimentoMensagem(string messageId, string body)
        {
            await HandleAsync(messageId, body, CancellationToken.None);
        }
    }
}