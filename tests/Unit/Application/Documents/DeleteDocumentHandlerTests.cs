using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Interfaces;
using S13G.Application.Documents.Commands;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class DeleteDocumentHandlerTests
    {
        private Mock<IFiscalDocumentRepository> _repoMock;
        private DeleteDocumentHandler _handler;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IFiscalDocumentRepository>();
            _handler = new DeleteDocumentHandler(_repoMock.Object);
        }

        [Test]
        public async Task Handle_ValidId_CallsDeleteAsyncWithCorrectId()
        {
            var id = Guid.NewGuid();

            await _handler.Handle(new DeleteDocumentCommand { Id = id }, CancellationToken.None);

            _repoMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
