using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.TaskDTOs.TaskFileDTOs
{
    public class TaskFileReadDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public int OrganizationId { get; set; }
        public int TaskId { get; set; }

        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }

        public string StorageKey { get; set; } = null!;
    }
}
