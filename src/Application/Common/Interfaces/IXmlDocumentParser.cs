using System.IO;
using System.Threading;
using System.Threading.Tasks;
using S13G.Domain.Entities;

namespace S13G.Application.Common.Interfaces
{
    public interface IXmlDocumentParser
    {
        /// <summary>
        /// Parse the supplied XML stream and return a partially-populated <see cref="FiscalDocument"/>.
        /// </summary>
        Task<FiscalDocument> ParseAsync(Stream xmlStream, CancellationToken ct = default);
    }
}