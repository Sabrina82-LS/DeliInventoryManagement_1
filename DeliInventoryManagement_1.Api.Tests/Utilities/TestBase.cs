using DeliInventoryManagement_1.Api.ModelsV5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeliInventoryManagement_1.Api.Tests.Utilities
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly Mock<ILogger> MockLogger;

        protected TestBase()
        {
            var services = new ServiceCollection();
            MockLogger = new Mock<ILogger>();
            services.AddSingleton(MockLogger.Object);
            ServiceProvider = services.BuildServiceProvider();
        }

        protected static Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }
        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
      
    }
}
