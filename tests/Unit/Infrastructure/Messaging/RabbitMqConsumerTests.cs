using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Interfaces;
using S13G.Application.Events;
using S13G.Domain.Entities;
using S13G.Infrastructure.Configuration;
using S13G.Infrastructure.Messaging;

namespace S13G.Tests.Unit.Infrastructure.Messaging
{
    [TestFixture]
    public class RabbitMqConsumerTests
    {
        private Mock<IDocumentSummaryRepository> _repoMock;
        private IServiceProvider _serviceProvider;
        private IOptions<RabbitMqOptions> _options;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IDocumentSummaryRepository>();

            // Wire up a real DI scope so CreateScope() resolves IDocumentSummaryRepository
            var services = new ServiceCollection();
            services.AddScoped(_ => _repoMock.Object);
            _serviceProvider = services.BuildServiceProvider();

            _options = Options.Create(new RabbitMqOptions
            {
                RetryCount = 2,
                RetryInitialDelayMs = 10
            });
        }

        private RabbitMqConsumer CreateConsumer() =>
            new RabbitMqConsumer(_options, NullLogger<RabbitMqConsumer>.Instance, _serviceProvider);

        [Test]
        public async Task HandleEvent_ValidEvent_CreatesSummaryWithCorrectFields()
        {
            var evt = new DocumentProcessedEvent
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = "NFe",
                Cnpj = "12345678000195",
                IssueDate = new DateTime(2024, 3, 10),
                ProcessedAt = DateTime.UtcNow,
                State = "SP"
            };

            DocumentSummary captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DocumentSummary>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentSummary, CancellationToken>((s, _) => captured = s)
                .Returns(Task.CompletedTask);

            var consumer = CreateConsumer();
            await consumer.HandleEvent(evt, CancellationToken.None);

            captured.Should().NotBeNull();
            captured.DocumentId.Should().Be(evt.DocumentId);
            captured.Type.Should().Be(DocumentType.NFe);
            captured.IssuerCnpj.Should().Be("12345678000195");
            captured.IssueDate.Should().Be(evt.IssueDate);
            captured.State.Should().Be("SP");
            captured.Id.Should().NotBe(Guid.Empty);
        }

        [Test]
        public async Task HandleEvent_NullState_DefaultsToEmptyString()
        {
            var evt = new DocumentProcessedEvent
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = "CTe",
                State = null
            };

            DocumentSummary captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DocumentSummary>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentSummary, CancellationToken>((s, _) => captured = s)
                .Returns(Task.CompletedTask);

            await CreateConsumer().HandleEvent(evt, CancellationToken.None);

            captured.State.Should().Be(string.Empty);
        }

        [Test]
        public async Task HandleEvent_EachInvocation_CreatesFreshScopeForRepo()
        {
            var evt = new DocumentProcessedEvent { DocumentId = Guid.NewGuid(), DocumentType = "NFe" };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DocumentSummary>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var consumer = CreateConsumer();
            await consumer.HandleEvent(evt, CancellationToken.None);
            await consumer.HandleEvent(evt, CancellationToken.None);

            // repository called once per HandleEvent invocation
            _repoMock.Verify(r => r.AddAsync(It.IsAny<DocumentSummary>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task HandleEvent_RepoThrows_ExceptionPropagates()
        {
            var evt = new DocumentProcessedEvent { DocumentId = Guid.NewGuid(), DocumentType = "NFe" };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<DocumentSummary>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db failure"));

            var consumer = CreateConsumer();
            var act = async () => await consumer.HandleEvent(evt, CancellationToken.None);

            // exception propagates to the caller (ExecuteAsync catches it and sends BasicNack)
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*db failure*");
        }
    }
}
