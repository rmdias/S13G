using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using S13G.Application.Common.Interfaces;
using S13G.Application.Common.Models;
using S13G.Application.Documents.Queries;
using S13G.Domain.Entities;

namespace S13G.Tests.Unit.Application.Documents
{
    [TestFixture]
    public class DocumentListHandlerTests
    {
        private Mock<IFiscalDocumentRepository> _repoMock;
        private DocumentListHandler _handler;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IFiscalDocumentRepository>();
            _repoMock.Setup(r => r.ListAsync(It.IsAny<DocumentFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<FiscalDocument> { Items = new List<FiscalDocument>(), Page = 1, PageSize = 20, TotalCount = 0 });
            _handler = new DocumentListHandler(_repoMock.Object);
        }

        [Test]
        public async Task Handle_WithCnpjFilter_PassesCnpjToRepository()
        {
            DocumentFilter captured = null;
            _repoMock.Setup(r => r.ListAsync(It.IsAny<DocumentFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentFilter, int, int, CancellationToken>((f, _, __, ___) => captured = f)
                .ReturnsAsync(new PaginatedResult<FiscalDocument> { Items = new List<FiscalDocument>() });

            await _handler.Handle(new DocumentListQuery { Cnpj = "12345678000195" }, CancellationToken.None);

            captured.Cnpj.Should().Be("12345678000195");
        }

        [Test]
        public async Task Handle_WithStateFilter_PassesStateToRepository()
        {
            DocumentFilter captured = null;
            _repoMock.Setup(r => r.ListAsync(It.IsAny<DocumentFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentFilter, int, int, CancellationToken>((f, _, __, ___) => captured = f)
                .ReturnsAsync(new PaginatedResult<FiscalDocument> { Items = new List<FiscalDocument>() });

            await _handler.Handle(new DocumentListQuery { State = "SP" }, CancellationToken.None);

            captured.State.Should().Be("SP");
        }

        [Test]
        public async Task Handle_WithDateRange_PassesDatesToRepository()
        {
            var from = new DateTime(2024, 1, 1);
            var to = new DateTime(2024, 12, 31);

            DocumentFilter captured = null;
            _repoMock.Setup(r => r.ListAsync(It.IsAny<DocumentFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentFilter, int, int, CancellationToken>((f, _, __, ___) => captured = f)
                .ReturnsAsync(new PaginatedResult<FiscalDocument> { Items = new List<FiscalDocument>() });

            await _handler.Handle(new DocumentListQuery { FromDate = from, ToDate = to }, CancellationToken.None);

            captured.FromDate.Should().Be(from);
            captured.ToDate.Should().Be(to);
        }

        [Test]
        public async Task Handle_Pagination_PassesPageParamsToRepository()
        {
            int capturedPage = 0, capturedSize = 0;
            _repoMock.Setup(r => r.ListAsync(It.IsAny<DocumentFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<DocumentFilter, int, int, CancellationToken>((_, p, s, __) => { capturedPage = p; capturedSize = s; })
                .ReturnsAsync(new PaginatedResult<FiscalDocument> { Items = new List<FiscalDocument>() });

            await _handler.Handle(new DocumentListQuery { Page = 3, PageSize = 50 }, CancellationToken.None);

            capturedPage.Should().Be(3);
            capturedSize.Should().Be(50);
        }
    }
}
