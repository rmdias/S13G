using System.Threading;
using System.Threading.Tasks;
using MediatR;
using S13G.Application.Common.Interfaces;
using S13G.Domain.Entities;

namespace S13G.Application.Documents.Commands
{
    public class UpdateDocumentHandler : IRequestHandler<UpdateDocumentCommand>
    {
        private readonly IFiscalDocumentRepository _repo;
        public UpdateDocumentHandler(IFiscalDocumentRepository repo) => _repo = repo;

        public async Task<Unit> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
        {
            var doc = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (doc == null) throw new KeyNotFoundException("Document not found");
            if (!string.IsNullOrWhiteSpace(request.State)) doc.State = request.State;
            if (!string.IsNullOrWhiteSpace(request.Status)) doc.Status = request.Status;
            await _repo.UpdateAsync(doc, cancellationToken);
            return Unit.Value;
        }
    }
}