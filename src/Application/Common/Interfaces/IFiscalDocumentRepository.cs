using System;
using System.Threading;
using System.Threading.Tasks;
using S13G.Domain.Entities;
using S13G.Application.Common.Models;

namespace S13G.Application.Common.Interfaces
{
    public interface IFiscalDocumentRepository
    {
        /// <summary>
        /// Adds document if an entry with the given idempotency key does not already exist.
        /// Returns the existing entity or the newly inserted one.
        /// </summary>
        Task<FiscalDocument> AddIfNotExistsAsync(FiscalDocument document, string idempotencyKey, CancellationToken ct = default);
        Task<FiscalDocument> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsKeyAsync(string keyHash, CancellationToken ct = default);

        // additional operations for API
        Task<PaginatedResult<FiscalDocument>> ListAsync(DocumentFilter filter, int page, int pageSize, CancellationToken ct = default);
        Task UpdateAsync(FiscalDocument document, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}