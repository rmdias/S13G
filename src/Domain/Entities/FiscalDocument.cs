using System;
using System.Collections.Generic;

namespace S13G.Domain.Entities
{
    public enum DocumentType { NFe, CTe, NFSe }

    public class FiscalDocument
    {
        public Guid Id { get; set; }
        /// <summary>Original document identifier (chave, NF number, etc.)</summary>
        public string DocumentKey { get; set; }
        public DocumentType Type { get; set; }    // enum { NFe, CTe, NFSe }
        public string IssuerCnpj { get; set; }
        public string RecipientCnpj { get; set; }
        public DateTime IssueDate { get; set; }
        public string State { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; }
        public string RawXml { get; set; }

        public DocumentKey Key { get; set; }
        public List<ProcessingEvent> Events { get; set; } = new();
    }
}