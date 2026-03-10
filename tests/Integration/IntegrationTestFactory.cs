using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using S13G.Infrastructure.Persistence;
using System.Linq;

namespace S13G.Tests.Integration
{
    public class IntegrationTestFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbConnection;
        private readonly string _rabbitConnection;

        public IntegrationTestFactory(string dbConnection, string rabbitConnection)
        {
            _dbConnection = dbConnection;
            _rabbitConnection = rabbitConnection;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = _dbConnection,
                    ["RabbitMq:HostName"] = _rabbitConnection,
                    ["RabbitMq:UserName"] = "guest",
                    ["RabbitMq:Password"] = "guest"
                };
                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureServices(services =>
            {
                // override DB context to use provided connection
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(_dbConnection));
            });
        }
    }
}