using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.WorkSessionDTOs
{
    public class WorkSessionHistoryDto
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }

        public int DurationSeconds { get; set; }
        public int Status { get; set; }
    }
}
