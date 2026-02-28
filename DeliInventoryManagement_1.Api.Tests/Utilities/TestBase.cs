using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

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

        protected Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}