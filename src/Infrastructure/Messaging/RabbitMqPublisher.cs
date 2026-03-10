using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using S13G.Application.Common.Interfaces;
using S13G.Infrastructure.Configuration;

namespace S13G.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IEventPublisher, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly RabbitMqOptions _options;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RabbitMqPublisher(IOptions<RabbitMqOptions> opts, ILogger<RabbitMqPublisher> logger)
        {
            _options = opts.Value;
            _logger = logger;

            _factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_options.RetryCount,
                    attempt => TimeSpan.FromMilliseconds(_options.RetryInitialDelayMs * Math.Pow(2, attempt - 1)),
                    (ex, ts) => _logger.LogWarning(ex, "Publish attempt failed, retrying after {delay}", ts));

            Connect();
        }

        /// <summary>Internal constructor for unit testing — bypasses Connect() and uses a pre-built channel.</summary>
        internal RabbitMqPublisher(IOptions<RabbitMqOptions> opts, ILogger<RabbitMqPublisher> logger, IModel channel)
        {
            _options = opts.Value;
            _logger = logger;
            _channel = channel;
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_options.RetryCount,
                    attempt => TimeSpan.FromMilliseconds(_options.RetryInitialDelayMs * Math.Pow(2, attempt - 1)),
                    (ex, ts) => _logger.LogWarning(ex, "Publish attempt failed, retrying after {delay}", ts));
        }

        private void Connect()
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(_options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _options.DeadLetterExchange }
                });
            _channel.QueueBind(_options.QueueName, _options.ExchangeName, string.Empty);

            // dead letter exchange
            _channel.ExchangeDeclare(_options.DeadLetterExchange, ExchangeType.Fanout, durable: true);

            // enable publisher confirms — broker will ACK/NACK each message
            _channel.ConfirmSelect();
        }

        public Task PublishAsync<T>(string routingKey, T message)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            return _retryPolicy.ExecuteAsync(() =>
            {
                _channel.BasicPublish(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);
                // wait for broker ACK; throws OperationInterruptedException on NACK or TimeoutException on timeout
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch { }
        }
    }
}