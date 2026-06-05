using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class MemberTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = null!;
        public string? TeamDescription { get; set; }
        public bool TeamIsActive { get; set; }
        public string? TeamRole { get; set; }
        public DateTime JoinedAt { get; set; }
        public int ProjectId { get; set; }
    }
}
