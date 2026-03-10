using System;

namespace S13G.Api.Models
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string DocumentKey { get; set; }
        public string DocumentType { get; set; }
        public string IssuerCnpj { get; set; }
        public string RecipientCnpj { get; set; }
        public DateTime IssueDate { get; set; }
        public string State { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; }
        public string RawXml { get; set; }
    }
}