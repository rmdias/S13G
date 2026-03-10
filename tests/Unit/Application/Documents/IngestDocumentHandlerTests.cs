using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Exceptions;
using S13G.Application.Common.Interfaces;
using S13G.Application.Documents.IngestDocument;
using S13G.Application.Events;
using S13G.Domain.Entities;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class IngestDocumentHandlerTests
    {
        private Mock<IXmlDocumentParser> _parserMock;
        private Mock<IXmlSchemaValidator> _validatorMock;
        private Mock<IFiscalDocumentRepository> _repoMock;
        private Mock<IEventPublisher> _publisherMock;
        private IngestDocumentHandler _handler;

        [SetUp]
        public void Setup()
        {
            _parserMock = new Mock<IXmlDocumentParser>();
            _validatorMock = new Mock<IXmlSchemaValidator>();
            _repoMock = new Mock<IFiscalDocumentRepository>();
            _publisherMock = new Mock<IEventPublisher>();

            // default: validation passes
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new XmlValidationResult(true, Array.Empty<string>()));

            _handler = new IngestDocumentHandler(
                _parserMock.Object, _validatorMock.Object,
                _repoMock.Object, _publisherMock.Object);
        }

        [Test]
        public async Task Handle_NewDocument_ReturnsDocumentIdAndPublishesEvent()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            var resultId = await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            resultId.Should().Be(doc.Id);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task Handle_InvalidXml_ThrowsXmlValidationExceptionAndNeverCallsRepo()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new XmlValidationResult(false, new[] { "XML is not well-formed: unexpected end of file" }));

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<broken"));
            var act = async () => await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            await act.Should().ThrowAsync<XmlValidationException>()
                .WithMessage("*XML is not well-formed*");
            _repoMock.Verify(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Test]
        public async Task Handle_InvalidXml_ExceptionContainsAllErrors()
        {
            var errors = new[] { "First error", "Second error" };
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new XmlValidationResult(false, errors));

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<bad/>"));
            var act = async () => await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            var ex = await act.Should().ThrowAsync<XmlValidationException>();
            ex.Which.Errors.Should().BeEquivalentTo(errors);
        }

        [Test]
        public async Task Handle_ParsedDocument_StatusIsSetToReceived()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            FiscalDocument capturedDoc = null;
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<FiscalDocument, string, CancellationToken>((d, _, __) => capturedDoc = d)
                .ReturnsAsync(doc);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            capturedDoc.Should().NotBeNull();
            capturedDoc.Status.Should().Be("Received");
        }

        [Test]
        public async Task Handle_NewDocument_PublishesCorrectEventPayload()
        {
            var docId = Guid.NewGuid();
            var doc = new FiscalDocument
            {
                Id = docId,
                Type = DocumentType.NFe,
                IssuerCnpj = "12345678000195",
                IssueDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                State = "SP"
            };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            object capturedEvent = null;
            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((_, evt) => capturedEvent = evt)
                .Returns(Task.CompletedTask);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            capturedEvent.Should().BeOfType<DocumentProcessedEvent>();
            var evt = (DocumentProcessedEvent)capturedEvent;
            evt.DocumentId.Should().Be(docId);
            evt.DocumentType.Should().Be("NFe");
            evt.Cnpj.Should().Be("12345678000195");
            evt.State.Should().Be("SP");
        }

        [Test]
        public async Task Handle_ExplicitIdempotencyKey_PassesKeyDirectlyToRepository()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            string capturedKey = null;
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<FiscalDocument, string, CancellationToken>((_, k, __) => capturedKey = k)
                .ReturnsAsync(doc);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            await _handler.Handle(new IngestDocumentCommand(ms, "my-explicit-key"), CancellationToken.None);

            capturedKey.Should().Be("my-explicit-key");
        }

        [Test]
        public async Task Handle_NullIdempotencyKey_ComputesSha256HexStringAsKey()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            string capturedKey = null;
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<FiscalDocument, string, CancellationToken>((_, k, __) => capturedKey = k)
                .ReturnsAsync(doc);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            await _handler.Handle(new IngestDocumentCommand(ms), CancellationToken.None);

            capturedKey.Should().NotBeNullOrEmpty();
            capturedKey.Should().HaveLength(64);
            capturedKey.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        [Test]
        public async Task Handle_SameXmlTwice_ProducesIdenticalHash()
        {
            const string xml = "<NFe><infNFe/></NFe>";
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            var capturedKeys = new List<string>();
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<FiscalDocument, string, CancellationToken>((_, k, __) => capturedKeys.Add(k))
                .ReturnsAsync(doc);

            using var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            await _handler.Handle(new IngestDocumentCommand(ms1), CancellationToken.None);
            using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            await _handler.Handle(new IngestDocumentCommand(ms2), CancellationToken.None);

            capturedKeys[0].Should().Be(capturedKeys[1]);
        }

        [Test]
        public async Task Handle_DuplicateKey_StillPublishesEvent()
        {
            // The handler always publishes after AddIfNotExistsAsync — downstream consumers are expected to be idempotent.
            var doc = new FiscalDocument { Id = Guid.NewGuid() };
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(doc);
            _repoMock.Setup(r => r.AddIfNotExistsAsync(It.IsAny<FiscalDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("<NFe><infNFe/></NFe>"));
            await _handler.Handle(new IngestDocumentCommand(ms, "same-key"), CancellationToken.None);

            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
    }
}
