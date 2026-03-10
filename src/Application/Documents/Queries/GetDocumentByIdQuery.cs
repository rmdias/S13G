using System;
using MediatR;
using S13G.Domain.Entities;

namespace S13G.Application.Documents.Queries
{
    public class GetDocumentByIdQuery : IRequest<FiscalDocument>
    {
        public Guid Id { get; set; }
    }
}