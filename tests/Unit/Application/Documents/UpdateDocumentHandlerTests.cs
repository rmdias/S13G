using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Interfaces;
using S13G.Application.Documents.Commands;
using S13G.Domain.Entities;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class UpdateDocumentHandlerTests
    {
        private Mock<IFiscalDocumentRepository> _repoMock;
        private UpdateDocumentHandler _handler;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IFiscalDocumentRepository>();
            _handler = new UpdateDocumentHandler(_repoMock.Object);
        }

        [Test]
        public async Task Handle_DocumentNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FiscalDocument)null);

            var act = async () => await _handler.Handle(
                new UpdateDocumentCommand { Id = Guid.NewGuid(), State = "SP" },
                CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*not found*");
        }

        [Test]
        public async Task Handle_BothStateAndStatus_BothUpdated()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid(), State = "RJ", Status = "Received" };
            _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            await _handler.Handle(
                new UpdateDocumentCommand { Id = doc.Id, State = "SP", Status = "Processed" },
                CancellationToken.None);

            doc.State.Should().Be("SP");
            doc.Status.Should().Be("Processed");
            _repoMock.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_OnlyState_StatusPreserved()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid(), State = "RJ", Status = "Received" };
            _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            await _handler.Handle(
                new UpdateDocumentCommand { Id = doc.Id, State = "SP", Status = null },
                CancellationToken.None);

            doc.State.Should().Be("SP");
            doc.Status.Should().Be("Received");
        }

        [Test]
        public async Task Handle_OnlyStatus_StatePreserved()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid(), State = "RJ", Status = "Received" };
            _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            await _handler.Handle(
                new UpdateDocumentCommand { Id = doc.Id, State = null, Status = "Processed" },
                CancellationToken.None);

            doc.State.Should().Be("RJ");
            doc.Status.Should().Be("Processed");
        }
    }
}
