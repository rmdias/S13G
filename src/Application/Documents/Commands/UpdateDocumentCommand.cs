using System;
using MediatR;

namespace S13G.Application.Documents.Commands
{
    public class UpdateDocumentCommand : IRequest
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
    }
}