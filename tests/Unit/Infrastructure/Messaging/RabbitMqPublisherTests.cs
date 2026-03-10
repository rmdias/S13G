using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using S13G.Infrastructure.Configuration;
using S13G.Infrastructure.Messaging;

namespace S13G.Tests.Unit.Infrastructure.Messaging
{
    [TestFixture]
    public class RabbitMqPublisherTests
    {
        private Mock<IModel> _channelMock;
        private Mock<IBasicProperties> _propertiesMock;
        private IOptions<RabbitMqOptions> _options;

        [SetUp]
        public void Setup()
        {
            _propertiesMock = new Mock<IBasicProperties>();
            _channelMock = new Mock<IModel>();
            _channelMock.Setup(c => c.CreateBasicProperties()).Returns(_propertiesMock.Object);

            _options = Options.Create(new RabbitMqOptions
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeName = "documents",
                QueueName = "documents.processed",
                DeadLetterExchange = "documents.dlx",
                RetryCount = 3,
                RetryInitialDelayMs = 10 // keep tests fast
            });
        }

        private RabbitMqPublisher CreatePublisher() =>
            new RabbitMqPublisher(_options, NullLogger<RabbitMqPublisher>.Instance, _channelMock.Object);

        [Test]
        public async Task PublishAsync_ValidMessage_CallsBasicPublishAndWaitsForConfirm()
        {
            var publisher = CreatePublisher();

            await publisher.PublishAsync(string.Empty, new { test = "data" });

            _channelMock.Verify(c => c.BasicPublish(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(),
                It.IsAny<System.ReadOnlyMemory<byte>>()),
                Times.Once);
            _channelMock.Verify(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()), Times.Once);
        }

        [Test]
        public async Task PublishAsync_ValidMessage_MessageMarkedAsPersistent()
        {
            var publisher = CreatePublisher();

            await publisher.PublishAsync(string.Empty, new { test = "data" });

            _propertiesMock.VerifySet(p => p.Persistent = true);
        }

        [Test]
        public async Task PublishAsync_TransientFailureThenSuccess_RetriesAndSucceeds()
        {
            var callCount = 0;
            _channelMock.Setup(c => c.BasicPublish(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<IBasicProperties>(),
                    It.IsAny<System.ReadOnlyMemory<byte>>()))
                .Callback(() =>
                {
                    callCount++;
                    if (callCount < 3)
                        throw new Exception("transient broker error");
                });

            var publisher = CreatePublisher();
            var act = async () => await publisher.PublishAsync(string.Empty, new { test = "data" });

            await act.Should().NotThrowAsync("retry policy should absorb transient failures");
            callCount.Should().Be(3);
        }

        [Test]
        public async Task PublishAsync_PersistentFailure_ThrowsAfterRetryCountExhausted()
        {
            _channelMock.Setup(c => c.BasicPublish(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<IBasicProperties>(),
                    It.IsAny<System.ReadOnlyMemory<byte>>()))
                .Throws(new Exception("broker unavailable"));

            var publisher = CreatePublisher();
            var act = async () => await publisher.PublishAsync(string.Empty, new { test = "data" });

            await act.Should().ThrowAsync<Exception>().WithMessage("*broker unavailable*");
            // RetryCount = 3 means 1 initial attempt + 3 retries = 4 total calls
            _channelMock.Verify(c => c.BasicPublish(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<IBasicProperties>(),
                It.IsAny<System.ReadOnlyMemory<byte>>()),
                Times.Exactly(4));
        }

        [Test]
        public async Task PublishAsync_ConfirmTimeout_ThrowsAfterRetries()
        {
            _channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()))
                .Throws(new TimeoutException("confirm timeout"));

            var publisher = CreatePublisher();
            var act = async () => await publisher.PublishAsync(string.Empty, new { test = "data" });

            await act.Should().ThrowAsync<TimeoutException>();
        }
    }
}
