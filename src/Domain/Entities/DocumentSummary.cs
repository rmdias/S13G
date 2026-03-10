using System;

namespace S13G.Domain.Entities
{
    public class DocumentSummary
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public DocumentType Type { get; set; }
        public string IssuerCnpj { get; set; }
        public string State { get; set; }
        public DateTime IssueDate { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}