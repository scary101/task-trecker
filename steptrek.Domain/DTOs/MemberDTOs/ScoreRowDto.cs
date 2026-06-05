using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class ScoreRowDto
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = null!;

        public int CompletedCount { get; set; }
        public int MissedCount { get; set; }
        public int TotalAssignedCount { get; set; }

        public decimal Trust { get; set; }
        public decimal MissRate { get; set; }
        public string TrustLevel { get; set; } = null!;
    }
}
