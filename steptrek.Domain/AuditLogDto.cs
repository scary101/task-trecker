using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain
{
    public class AuditLogDto
    {
        public long Id { get; set; }

        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public long? EntityId { get; set; }
        public string? EntityName { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public int? ActorMemberId { get; set; }
        public string? ActorName { get; set; }

        public int? TargetMemberId { get; set; }

        public int? TeamId { get; set; }
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
