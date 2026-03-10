using System;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;

namespace S13G.Tests.Integration.Fixtures
{
    public class RabbitMqFixture : IAsyncDisposable
    {
        public RabbitMqContainer Container { get; }

        public RabbitMqFixture()
        {
            Container = new RabbitMqBuilder()
                .WithImage("rabbitmq:3-management-alpine")
                .Build();
        }

        public async Task InitializeAsync() => await Container.StartAsync();
        public async ValueTask DisposeAsync() => await Container.DisposeAsync();
    }
}
