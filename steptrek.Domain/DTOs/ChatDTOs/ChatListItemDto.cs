using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatListItemDto
    {
        public int TeamId { get; set; }
        public int ProjectId { get; set; }
        public string TeamName { get; set; } = "";
        public string ProjectName { get; set; } = "";

        public string? LastMessageText { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }
}
