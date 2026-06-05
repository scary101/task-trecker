using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }

        public bool HasNext => Page * PageSize < TotalCount;
    }
}
