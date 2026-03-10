using System.Threading;
using System.Threading.Tasks;
using MediatR;
using S13G.Application.Common.Interfaces;

namespace S13G.Application.Documents.Commands
{
    public class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand>
    {
        private readonly IFiscalDocumentRepository _repo;
        public DeleteDocumentHandler(IFiscalDocumentRepository repo) => _repo = repo;

        public async Task<Unit> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            await _repo.DeleteAsync(request.Id, cancellationToken);
            return Unit.Value;
        }
    }
}