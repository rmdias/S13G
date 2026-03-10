using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using S13G.Application.Common.Interfaces;
using S13G.Application.Common.Models;

namespace S13G.Application.Documents.Queries
{
    public class DocumentListHandler : IRequestHandler<DocumentListQuery, PaginatedResult<S13G.Domain.Entities.FiscalDocument>>
    {
        private readonly IFiscalDocumentRepository _repo;

        public DocumentListHandler(IFiscalDocumentRepository repo)
        {
            _repo = repo;
        }

        public async Task<PaginatedResult<S13G.Domain.Entities.FiscalDocument>> Handle(DocumentListQuery request, CancellationToken cancellationToken)
        {
            return await _repo.ListAsync(new DocumentFilter
            {
                Cnpj = request.Cnpj,
                State = request.State,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            }, request.Page, request.PageSize, cancellationToken);
        }
    }
}