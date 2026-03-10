using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using S13G.Application.Common.Exceptions;
using S13G.Application.Common.Interfaces;
using S13G.Domain.Entities;

namespace S13G.Application.Documents.IngestDocument
{
    public class IngestDocumentHandler : IRequestHandler<IngestDocumentCommand, Guid>
    {
        private readonly IXmlDocumentParser _parser;
        private readonly IXmlSchemaValidator _validator;
        private readonly IFiscalDocumentRepository _repository;
        private readonly IEventPublisher _publisher;

        public IngestDocumentHandler(IXmlDocumentParser parser, IXmlSchemaValidator validator, IFiscalDocumentRepository repository, IEventPublisher publisher)
        {
            _parser = parser;
            _validator = validator;
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
        {
            // read stream content once — needed for both validation and parsing
            string xmlContent;
            using (var sr = new StreamReader(request.XmlStream, leaveOpen: true))
                xmlContent = await sr.ReadToEndAsync(cancellationToken);

            // validate structure before persisting
            var validation = await _validator.ValidateAsync(xmlContent, cancellationToken);
            if (!validation.IsValid)
                throw new XmlValidationException(validation.Errors);

            // parse from the already-read content
            var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            var document = await _parser.ParseAsync(xmlStream, cancellationToken);
            document.Status = "Received";

            // determine idempotency key
            var key = request.IdempotencyKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                key = await ComputeHashAsync(request.XmlStream, cancellationToken);
            }

            // persist with dedup
            var result = await _repository.AddIfNotExistsAsync(document, key, cancellationToken);

            // publish event (fire-and-forget; failures will be handled by publisher policy)
            var evt = new Events.DocumentProcessedEvent
            {
                DocumentId = result.Id,
                DocumentType = result.Type.ToString(),
                Cnpj = result.IssuerCnpj,
                IssueDate = result.IssueDate,
                ProcessedAt = DateTime.UtcNow,
                State = result.State
            };
            await _publisher.PublishAsync(string.Empty, evt);

            return result.Id;
        }

        private async Task<string> ComputeHashAsync(Stream stream, CancellationToken ct)
        {
            // rewind if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, ct);
            if (stream.CanSeek)
                stream.Position = 0;

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}