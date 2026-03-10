using System;
using System.Text.Json.Serialization;

namespace S13G.Application.Events
{
    public class DocumentProcessedEvent
    {
        [JsonPropertyName("documentId")]
        public Guid DocumentId { get; set; }
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; }
        [JsonPropertyName("cnpj")]
        public string Cnpj { get; set; }
        [JsonPropertyName("issueDate")]
        public DateTime IssueDate { get; set; }
        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("state")]
        public string State { get; set; }
    }
}