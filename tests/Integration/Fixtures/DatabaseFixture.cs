using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace S13G.Tests.Integration.Fixtures
{
    public class DatabaseFixture : IAsyncDisposable
    {
        public PostgreSqlContainer Container { get; }

        public DatabaseFixture()
        {
            Container = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:15-alpine")
                .Build();
        }

        public async Task InitializeAsync() => await Container.StartAsync();
        public async ValueTask DisposeAsync() => await Container.DisposeAsync();
    }
}
