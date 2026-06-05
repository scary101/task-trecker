using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public partial class AuditLog
    {
        public int Id { get; set; }

        public string TableName { get; set; } = null!;

        public string Action { get; set; } = null!;

        public long? RecordId { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
