using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.CalendarDTOs
{
    public class CalendarEventDto
    {
        public string Id { get; set; } = null!;

        public int TeamId { get; set; }

        public int? TaskId { get; set; }

        public string Title { get; set; } = null!;

        public string Type { get; set; } = null!;

        public DateTime StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public bool IsPinned { get; set; }

        public string? Priority { get; set; }

        public string? Status { get; set; }
    }
}
