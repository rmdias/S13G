using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using S13G.Application.Common.Exceptions;
using S13G.Application.Documents.IngestDocument;
using S13G.Application.Documents.Commands;
using S13G.Application.Documents.Queries;
using S13G.Application.Common.Models;
using S13G.Api.Models;
using S13G.Domain.Entities;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace S13G.Api.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Upload and process fiscal XML document.
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, [FromHeader(Name="Idempotency-Key")] string key)
        {
            var file = request?.File;
            if (file == null || file.Length == 0)
                return BadRequest("XML file is required.");

            using var stream = file.OpenReadStream();
            var command = new IngestDocumentCommand(stream, key);
            try
            {
                var id = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { id }, new { id });
            }
            catch (XmlValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
        }

        /// <summary>
        /// Get paged list of documents with optional filters.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<DocumentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<DocumentDto>>> List(
            [FromQuery][Range(1, int.MaxValue)] int page = 1,
            [FromQuery][Range(1, 100)] int pageSize = 20,
            [FromQuery] string cnpj = null,
            [FromQuery] string uf = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var q = new DocumentListQuery
            {
                Page = page,
                PageSize = pageSize,
                Cnpj = cnpj,
                State = uf,
                FromDate = fromDate,
                ToDate = toDate
            };
            var result = await _mediator.Send(q);
            var dto = new PagedResultDto<DocumentDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                Items = result.Items.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    DocumentKey = d.DocumentKey,
                    DocumentType = d.Type.ToString(),
                    IssuerCnpj = d.IssuerCnpj,
                    RecipientCnpj = d.RecipientCnpj,
                    IssueDate = d.IssueDate,
                    State = d.State,
                    TotalValue = d.TotalValue,
                    Status = d.Status,
                    RawXml = d.RawXml
                })
            };
            return Ok(dto);
        }

        /// <summary>
        /// Get document details by id.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentDto>> GetById(Guid id)
        {
            var doc = await _mediator.Send(new GetDocumentByIdQuery { Id = id });
            if (doc == null) return NotFound();
            var dto = new DocumentDto
            {
                Id = doc.Id,
                DocumentKey = doc.DocumentKey,
                DocumentType = doc.Type.ToString(),
                IssuerCnpj = doc.IssuerCnpj,
                RecipientCnpj = doc.RecipientCnpj,
                IssueDate = doc.IssueDate,
                State = doc.State,
                TotalValue = doc.TotalValue,
                Status = doc.Status,
                RawXml = doc.RawXml
            };
            return Ok(dto);
        }

        /// <summary>
        /// Update document metadata (not raw XML).
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentRequest request)
        {
            var cmd = new UpdateDocumentCommand
            {
                Id = id,
                State = request.State,
                Status = request.Status
            };
            await _mediator.Send(cmd);
            return NoContent();
        }

        /// <summary>
        /// Delete a document permanently.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteDocumentCommand { Id = id });
            return NoContent();
        }
    }
}