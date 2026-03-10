using Microsoft.AspNetCore.Http;

namespace S13G.Api.Models
{
    public class UploadDocumentRequest
    {
        public IFormFile File { get; set; }
    }
}
