using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using S13G.Application.Common.Interfaces;
using S13G.Application.Common.Models;
using S13G.Domain.Entities;

namespace S13G.Infrastructure.Persistence
{
    public class FiscalDocumentRepository : IFiscalDocumentRepository
    {
        private readonly AppDbContext _context;

        public FiscalDocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FiscalDocument> AddIfNotExistsAsync(FiscalDocument document, string idempotencyKey, CancellationToken ct = default)
        {
            // check for existing key first (read-only)
            var existing = await _context.DocumentKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyHash == idempotencyKey, ct);

            if (existing != null)
            {
                return await _context.FiscalDocuments
                    .AsNoTracking()
                    .FirstAsync(d => d.Id == existing.DocumentId, ct);
            }

            // transaction ensures atomicity between document and key
            using var tx = await _context.Database.BeginTransactionAsync(ct);

            _context.FiscalDocuments.Add(document);
            _context.DocumentKeys.Add(new DocumentKey
            {
                KeyHash = idempotencyKey,
                Document = document
            });

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return document;
        }

        public async Task<FiscalDocument> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.FiscalDocuments.FindAsync(new object[] { id }, ct);
        }

        public async Task<bool> ExistsKeyAsync(string keyHash, CancellationToken ct = default)
        {
            return await _context.DocumentKeys.AnyAsync(k => k.KeyHash == keyHash, ct);
        }

        public async Task<PaginatedResult<FiscalDocument>> ListAsync(DocumentFilter filter, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.FiscalDocuments.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Cnpj))
                query = query.Where(d => d.IssuerCnpj == filter.Cnpj || d.RecipientCnpj == filter.Cnpj);
            if (!string.IsNullOrWhiteSpace(filter.State))
                query = query.Where(d => d.State == filter.State);
            if (filter.FromDate.HasValue)
                query = query.Where(d => d.IssueDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(d => d.IssueDate <= filter.ToDate.Value);

            var total = await query.LongCountAsync(ct);
            var items = await query
                .OrderByDescending(d => d.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PaginatedResult<FiscalDocument>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task UpdateAsync(FiscalDocument document, CancellationToken ct = default)
        {
            _context.FiscalDocuments.Update(document);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var d = await _context.FiscalDocuments.FindAsync(new object[] { id }, ct);
            if (d != null)
            {
                _context.FiscalDocuments.Remove(d);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}