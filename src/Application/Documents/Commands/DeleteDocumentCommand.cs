using System;
using MediatR;

namespace S13G.Application.Documents.Commands
{
    public class DeleteDocumentCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}