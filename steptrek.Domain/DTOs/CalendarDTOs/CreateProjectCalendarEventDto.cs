using steptreck.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.CalendarDTOs
{
    public class CreateProjectCalendarEventDto
    {
        public int TeamId { get; set; }

        public string Title { get; set; } = null!;

        public string Type { get; set; } = null!; 

        public DateTime StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public string? Description { get; set; }

        public bool IsPinned { get; set; }
    }
}
