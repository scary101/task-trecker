using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.NoteDTOs
{
    public class NoteDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public bool IsPinned { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
