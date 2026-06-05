using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs
{
    public sealed class TaskReadDto
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public int? TeamId { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;

        public DateTime? Deadline { get; set; }
        public bool IsArchived { get; set; }

        public string CreatedBy { get; set; } = null!;
        public string? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<TaskChecklistItemDto> Checklist { get; set; } = new();
    }

}
