using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using S13G.Application.Common.Interfaces;
using S13G.Application.Events;
using S13G.Infrastructure.Configuration;

namespace S13G.Infrastructure.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly RabbitMqOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IModel _channel;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RabbitMqConsumer(IOptions<RabbitMqOptions> opts,
                                 ILogger<RabbitMqConsumer> logger,
                                 IServiceProvider serviceProvider)
        {
            _options = opts.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_options.RetryCount,
                    attempt => TimeSpan.FromMilliseconds(_options.RetryInitialDelayMs * Math.Pow(2, attempt - 1)),
                    (ex, ts) => _logger.LogWarning(ex, "Consumer processing failed, retrying after {delay}", ts));

            // Don't connect in constructor - let it be handled in StartAsync
            // This allows the application to start even if RabbitMQ is unavailable
        }

        private void Connect()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, 1, false);
            // Declare exchange and queue so the consumer doesn't depend on the
            // publisher having been instantiated first (QueueDeclare is idempotent)
            _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(_options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _options.DeadLetterExchange }
                });
            _channel.QueueBind(_options.QueueName, _options.ExchangeName, string.Empty);
            _channel.ExchangeDeclare(_options.DeadLetterExchange, ExchangeType.Fanout, durable: true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Try to connect if not already connected
                if (_connection == null || _channel == null)
                {
                    Connect();
                }

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    try
                    {
                        var evt = JsonSerializer.Deserialize<DocumentProcessedEvent>(body);
                        await _retryPolicy.ExecuteAsync(async () =>
                        {
                            await HandleEvent(evt, stoppingToken);
                        });
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message, sending to DLX");
                        _channel.BasicNack(ea.DeliveryTag, false, false); // send to DLX
                    }
                };

                _channel.BasicConsume(queue: _options.QueueName,
                                      autoAck: false,
                                      consumer: consumer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ Consumer failed to start. The application will continue without message processing. Please ensure RabbitMQ is running at {hostname}:{port}", _options.HostName, 5672);
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        internal async Task HandleEvent(DocumentProcessedEvent evt, CancellationToken ct)
        {
            var summary = new Domain.Entities.DocumentSummary
            {
                Id = Guid.NewGuid(),
                DocumentId = evt.DocumentId,
                Type = Enum.Parse<Domain.Entities.DocumentType>(evt.DocumentType),
                IssuerCnpj = evt.Cnpj,
                IssueDate = evt.IssueDate,
                ProcessedAt = evt.ProcessedAt,
                State = evt.State ?? string.Empty
            };

            // create a scope for each message to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDocumentSummaryRepository>();
            await repo.AddAsync(summary, ct);
        }

        public override void Dispose()
        {
            try { _channel?.Close(); _connection?.Close(); }
            catch { }
            base.Dispose();
        }
    }
}