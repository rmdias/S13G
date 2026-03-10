using System.IO;
using MediatR;

namespace S13G.Application.Documents.IngestDocument
{
    public class IngestDocumentCommand : IRequest<System.Guid>
    {
        public Stream XmlStream { get; }
        public string IdempotencyKey { get; }

        public IngestDocumentCommand(Stream xmlStream, string idempotencyKey = null)
        {
            XmlStream = xmlStream;
            IdempotencyKey = idempotencyKey;
        }
    }
}