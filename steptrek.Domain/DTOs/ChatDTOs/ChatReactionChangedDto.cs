using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatReactionChangedDto
    {
        public long MessageId { get; set; }
        public int TeamId { get; set; }
        public string Emoji { get; set; } = "";
        public int Count { get; set; }
        public bool IsAdded { get; set; }
        public int MemberId { get; set; }
    }
}
