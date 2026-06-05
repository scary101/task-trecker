using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.ProjectDTOs
{
    public sealed class ProjectLeadDto
    {
        public int MemberId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string RoleInTeam { get; set; }
    }

}
