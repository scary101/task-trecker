using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class MemberProfileDto
    {
        public int Id { get; set; }

        public string OrganizationName { get; set; }
        public int? UserId { get; set; }

        public string Surname { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Patronymic { get; set; }

        public string FullName { get; set; } = null!;
        public string? Username { get; set; }

        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;

        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public MemberTeamDto? Team { get; set; }
    }
}
