using steptreck.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatMessageDto
    {
        public long Id { get; set; }
        public int TeamId { get; set; }
        public int SenderMemberId { get; set; }
        public string SenderName { get; set; } = "";
        public string Text { get; set; } = "";
        public int Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public int? PinnedByMemberId { get; set; }
        public List<ChatReactionDto> Reactions { get; set; } = [];
        public long? ReplyToMessageId { get; set; }
        public ChatReplyDto? ReplyTo { get; set; }
    }
}
