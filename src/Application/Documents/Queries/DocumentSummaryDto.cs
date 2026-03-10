using System;

namespace S13G.Application.Documents.Queries
{
    public class DocumentSummaryDto
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; }
        public string IssuerCnpj { get; set; }
        public string State { get; set; }
        public DateTime IssueDate { get; set; }
    }
}