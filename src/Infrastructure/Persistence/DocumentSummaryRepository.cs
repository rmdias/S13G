using System.Threading;
using System.Threading.Tasks;
using S13G.Application.Common.Interfaces;
using S13G.Domain.Entities;

namespace S13G.Infrastructure.Persistence
{
    public class DocumentSummaryRepository : IDocumentSummaryRepository
    {
        private readonly AppDbContext _context;
        public DocumentSummaryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DocumentSummary summary, CancellationToken ct = default)
        {
            _context.Add(summary);
            await _context.SaveChangesAsync(ct);
        }
    }
}