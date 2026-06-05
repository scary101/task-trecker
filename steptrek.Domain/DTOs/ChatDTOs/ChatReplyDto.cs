using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatReplyDto
    {
        public long Id { get; set; }
        public string? Text { get; set; }
        public string SenderName { get; set; } = "";
    }
}
