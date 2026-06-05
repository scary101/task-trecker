using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class TaskListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public DateTime? Deadline { get; set; }
        public int ProjectId { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? AssignedTo { get; set; }
    }
}
