using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class ScoreDto
    {
        public int MemberId { get; set; }

        public int CompletedCount { get; set; }
        public int MissedCount { get; set; }
        public int TotalAssignedCount { get; set; }

        public decimal Trust { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public decimal MissRate { get; set; }         
        public string TrustLevel { get; set; } = null!;

        public List<ScoreLogDto> LogScore { get; set; } = new();

        public List<ScorePriorityStatDto> PriorityStats { get; set; } = new();
    }

    public class ScorePriorityStatDto
    {
        public int PriorityId { get; set; }

        public string PriorityTitle { get; set; } = "";

        public int CompletedCount { get; set; }

        public int MissedCount { get; set; }

        public int TotalCount { get; set; }
    }

    public class ScoreLogDto
    {
        public decimal Delta { get; set; }

        public bool IsIncrease { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
