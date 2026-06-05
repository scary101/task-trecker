using System;

namespace steptreck.Domain.DTOs.TeamDTOs
{
    public class TeamReadDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? CardBackgroundUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int? LeadMemberId { get; set; }
        public string? LeadFullName { get; set; }
        public string? LeadAvatarUrl { get; set; }
    }
}
