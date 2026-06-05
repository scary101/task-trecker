using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatPinChangedDto
    {
        public long MessageId { get; set; }
        public int TeamId { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public int? PinnedByMemberId { get; set; }
    }
}
