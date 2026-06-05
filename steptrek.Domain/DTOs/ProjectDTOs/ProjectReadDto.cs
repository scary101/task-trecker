using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ProjectDTOs
{
    public class ProjectReadDto
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? GitUrl { get; set; }
        public string? CardBackgroundUrl { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}
