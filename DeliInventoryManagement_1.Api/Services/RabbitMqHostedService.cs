using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeliInventoryManagement_1.Api.Services
{
    public class RabbitMqHostedService : IHostedService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<RabbitMqHostedService> _logger;

        public RabbitMqHostedService(IRabbitMqService rabbitMqService, ILogger<RabbitMqHostedService> logger)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 Starting RabbitMQ Hosted Service...");
                await _rabbitMqService.ConnectAsync();
                await _rabbitMqService.CreateQueuesAsync();
                _logger.LogInformation("✅ RabbitMQ Hosted Service started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start RabbitMQ");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Stopping RabbitMQ Hosted Service...");
            await _rabbitMqService.DisconnectAsync();
            _logger.LogInformation("✅ RabbitMQ Hosted Service stopped");
        }
    }
}