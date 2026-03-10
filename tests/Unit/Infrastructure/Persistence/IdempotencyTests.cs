using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using S13G.Infrastructure.Persistence;
using S13G.Domain.Entities;

namespace S13G.Tests.Unit.Infrastructure.Persistence
{
    [TestFixture]
    public class IdempotencyTests
    {
        private AppDbContext _context;
        private FiscalDocumentRepository _repo;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new AppDbContext(options);
            _repo = new FiscalDocumentRepository(_context);
        }

        [Test]
        public async Task AddIfNotExistsAsync_SameKeyTwice_ReturnsSameDocumentWithoutDuplicate()
        {
            var doc = new FiscalDocument
            {
                Id = Guid.NewGuid(),
                DocumentKey = "KEY001",
                IssuerCnpj = "12345678000195",
                RecipientCnpj = "98765432000100",
                RawXml = "<NFe/>",
                State = "SP",
                Status = "Received"
            };
            var key = "abc123";

            var first = await _repo.AddIfNotExistsAsync(doc, key, default);
            var second = await _repo.AddIfNotExistsAsync(new FiscalDocument
            {
                Id = Guid.NewGuid(),
                DocumentKey = "KEY002",
                IssuerCnpj = "11111111000100",
                RecipientCnpj = "22222222000100",
                RawXml = "<NFe/>",
                State = "RJ",
                Status = "Received"
            }, key, default);

            second.Id.Should().Be(first.Id);
            (await _context.FiscalDocuments.CountAsync()).Should().Be(1);
            (await _context.DocumentKeys.CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task AddIfNotExistsAsync_ConcurrentReadsAfterFirstInsert_AllReturnSameId()
        {
            // Seed the first document so the key already exists
            var originalDoc = new FiscalDocument
            {
                Id = Guid.NewGuid(),
                DocumentKey = "KEY-CONCURRENT",
                IssuerCnpj = "12345678000195",
                RecipientCnpj = string.Empty,
                RawXml = "<NFe/>",
                State = "SP",
                Status = "Received"
            };
            const string key = "concurrent-key";
            await _repo.AddIfNotExistsAsync(originalDoc, key, default);

            // Fire 5 concurrent lookups — all hit the "key already exists" fast path
            var tasks = Enumerable.Range(0, 5).Select(_ =>
                _repo.AddIfNotExistsAsync(new FiscalDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentKey = "KEY-DUPLICATE",
                    IssuerCnpj = "99999999000100",
                    RecipientCnpj = string.Empty,
                    RawXml = "<NFe/>",
                    State = "RJ",
                    Status = "Received"
                }, key, default));

            var results = await Task.WhenAll(tasks);

            results.Should().AllSatisfy(r => r.Id.Should().Be(originalDoc.Id));
            (await _context.FiscalDocuments.CountAsync()).Should().Be(1);
            (await _context.DocumentKeys.CountAsync()).Should().Be(1);
        }
    }
}
