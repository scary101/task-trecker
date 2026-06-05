using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ChatDTOs
{
    public class ChatReactionDto
    {
        public string Emoji { get; set; } = "";
        public int Count { get; set; }
        public bool ReactedByMe { get; set; }
    }
}
