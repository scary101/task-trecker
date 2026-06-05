using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.SubscriptionsDTOs
{
    public sealed class CurrentSubscriptionDto
    {
        public bool HasActive { get; set; } 
        public int? SubscriptionId { get; set; }
        public int? PlanId { get; set; }
        public string? PlanName { get; set; }
        public string? StatusCode { get; set; }

        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }
        public int? DaysLeft { get; set; } 
        public bool IsExpired { get; set; }

        public string? Currency { get; set; }
        public int? PriceCentsPerMonth { get; set; }

        public int? MaxMembers { get; set; }
        public int? MaxTeams { get; set; }
        public int? MaxProjects { get; set; }

        public int MembersCount { get; set; }
        public int TeamsCount { get; set; }
        public int ProjectsCount { get; set; }

        public int? MembersLeft { get; set; }
        public int? TeamsLeft { get; set; }
        public int? ProjectsLeft { get; set; }

        public bool AllowInvites { get; set; }
        public bool AllowNewProjects { get; set; }
        public bool AllowNewTeams { get; set; }

        public bool CanInviteMember { get; set; }
        public bool CanCreateTeam { get; set; }
        public bool CanCreateProject { get; set; }
    }
}
