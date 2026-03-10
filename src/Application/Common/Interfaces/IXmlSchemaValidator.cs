using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S13G.Application.Common.Interfaces
{
    public record XmlValidationResult(bool IsValid, IReadOnlyList<string> Errors);

    public interface IXmlSchemaValidator
    {
        Task<XmlValidationResult> ValidateAsync(string xmlContent, CancellationToken ct = default);
    }
}
