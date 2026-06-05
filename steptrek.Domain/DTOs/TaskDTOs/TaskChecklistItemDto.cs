using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class TaskChecklistItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public bool IsDone { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

}
