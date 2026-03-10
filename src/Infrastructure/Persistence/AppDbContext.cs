using Microsoft.EntityFrameworkCore;
using S13G.Domain.Entities;

namespace S13G.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<FiscalDocument> FiscalDocuments { get; set; }
        public DbSet<DocumentKey> DocumentKeys { get; set; }
        public DbSet<ProcessingEvent> ProcessingEvents { get; set; }
        public DbSet<DocumentSummary> DocumentSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FiscalDocument>(b =>
            {
                b.HasKey(d => d.Id);
                b.Property(d => d.Type).HasConversion<string>().IsRequired();
                b.HasOne(d => d.Key)
                    .WithOne(k => k.Document)
                    .HasForeignKey<DocumentKey>(k => k.DocumentId);
                b.HasMany(d => d.Events)
                    .WithOne(e => e.Document)
                    .HasForeignKey(e => e.DocumentId);
                b.HasIndex(d => d.IssuerCnpj);
                b.HasIndex(d => d.RecipientCnpj);
                b.HasIndex(d => d.State);
                b.HasIndex(d => d.IssueDate);
            });

            modelBuilder.Entity<DocumentKey>(b =>
            {
                b.HasKey(k => k.KeyHash);
                b.Property(k => k.KeyHash).HasMaxLength(64);
                b.HasIndex(k => k.KeyHash).IsUnique();
            });

            modelBuilder.Entity<ProcessingEvent>(b =>
            {
                b.HasKey(e => e.Id);
            });

            modelBuilder.Entity<DocumentSummary>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Type).HasConversion<string>().IsRequired();
                b.HasIndex(s => s.DocumentId).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}