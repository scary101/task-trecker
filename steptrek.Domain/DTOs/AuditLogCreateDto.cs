using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public class AuditLogCreateDto
    {
        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }

        public int? TargetMemberId { get; set; }

        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public long? EntityId { get; set; }

        public string? EntityName { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public object? Metadata { get; set; }
    }
}
