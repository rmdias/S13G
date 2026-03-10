using System;

namespace S13G.Domain.Entities
{
    public class ProcessingEvent
    {
        public long Id { get; set; }
        public Guid DocumentId { get; set; }
        public FiscalDocument Document { get; set; }
        public string EventType { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string Payload { get; set; }
    }
}