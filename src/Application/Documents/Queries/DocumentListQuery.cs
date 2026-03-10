using System;
using MediatR;
using S13G.Application.Common.Models;

namespace S13G.Application.Documents.Queries
{
    public class DocumentListQuery : IRequest<PaginatedResult<S13G.Domain.Entities.FiscalDocument>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Cnpj { get; set; }
        public string State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}