using FluentValidation;
using S13G.Api.Models;

namespace S13G.Api.Validators
{
    public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
    {
        public UpdateDocumentRequestValidator()
        {
            RuleFor(x => x.State)
                .MaximumLength(2)
                .When(x => x.State != null);

            RuleFor(x => x.Status)
                .MaximumLength(50)
                .When(x => x.Status != null);
        }
    }
}