using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public string? FileUrl { get; set; }
    }
}
