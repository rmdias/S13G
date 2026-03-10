using System.Collections.Generic;

namespace S13G.Api.Models
{
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
    }
}