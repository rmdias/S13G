using System.Threading;
using System.Threading.Tasks;
using MediatR;
using S13G.Application.Common.Interfaces;
using S13G.Domain.Entities;

namespace S13G.Application.Documents.Queries
{
    public class GetDocumentByIdHandler : IRequestHandler<GetDocumentByIdQuery, FiscalDocument>
    {
        private readonly IFiscalDocumentRepository _repo;
        public GetDocumentByIdHandler(IFiscalDocumentRepository repo)
        {
            _repo = repo;
        }

        public Task<FiscalDocument> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
        {
            return _repo.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}