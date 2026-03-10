using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Interfaces;
using S13G.Application.Documents.Queries;
using S13G.Domain.Entities;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class GetDocumentByIdHandlerTests
    {
        private Mock<IFiscalDocumentRepository> _repoMock;
        private GetDocumentByIdHandler _handler;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IFiscalDocumentRepository>();
            _handler = new GetDocumentByIdHandler(_repoMock.Object);
        }

        [Test]
        public async Task Handle_ExistingId_ReturnsDocument()
        {
            var doc = new FiscalDocument { Id = Guid.NewGuid(), IssuerCnpj = "12345678000195" };
            _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

            var result = await _handler.Handle(new GetDocumentByIdQuery { Id = doc.Id }, CancellationToken.None);

            result.Should().Be(doc);
        }

        [Test]
        public async Task Handle_NonExistingId_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FiscalDocument)null);

            var result = await _handler.Handle(new GetDocumentByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
