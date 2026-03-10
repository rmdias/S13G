using System;

namespace S13G.Application.Common.Models
{
    public class DocumentFilter
    {
        public string Cnpj { get; set; }
        public string State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}