using System.Collections.Generic;

namespace S13G.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
    }
}