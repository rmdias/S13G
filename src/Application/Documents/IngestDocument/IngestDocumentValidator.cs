using FluentValidation;

namespace S13G.Application.Documents.IngestDocument
{
    public class IngestDocumentValidator : AbstractValidator<IngestDocumentCommand>
    {
        public IngestDocumentValidator()
        {
            RuleFor(x => x.XmlStream)
                .NotNull().WithMessage("XML payload is required.")
                .Must(s => s != null && s.CanRead).WithMessage("XML stream must be readable.");
        }
    }
}