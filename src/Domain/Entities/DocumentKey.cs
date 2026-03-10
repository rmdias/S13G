using System;

namespace S13G.Domain.Entities
{
    public class DocumentKey
    {
        public string KeyHash { get; set; }
        public Guid DocumentId { get; set; }
        public FiscalDocument Document { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}