using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.PlanDTOs
{
    public class PlanPublicDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Currency { get; set; } = "RUB";
        public int BasePriceCents { get; set; }
        public int MinMembers { get; set; }
        public int MinProjects { get; set; }
        public int MinTeams { get; set; }
        public int MaxMembers { get; set; }
        public int MaxProjects { get; set; }
        public int MaxTeams { get; set; }
        public bool AllowInvites { get; set; }
        public bool AllowNewProjects { get; set; }
        public bool AllowNewTeams { get; set; }
    }
}
