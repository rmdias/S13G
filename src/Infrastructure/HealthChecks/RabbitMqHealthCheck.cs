using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using S13G.Infrastructure.Configuration;

namespace S13G.Infrastructure.HealthChecks
{
    public class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly RabbitMqOptions _options;

        public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost ?? "/"
                };
                using var conn = factory.CreateConnection();
                return Task.FromResult(conn.IsOpen
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("RabbitMQ connection is not open"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex));
            }
        }
    }
}
