using System;
using System.Threading;
using System.Threading.Tasks;
using S13G.Domain.Entities;

namespace S13G.Application.Common.Interfaces
{
    public interface IDocumentSummaryRepository
    {
        Task AddAsync(DocumentSummary summary, CancellationToken ct = default);
    }
}