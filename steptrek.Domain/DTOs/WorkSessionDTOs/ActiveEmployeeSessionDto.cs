using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.WorkSessionDTOs
{
    public class ActiveEmployeeSessionDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int CurrentStatus { get; set; }
        public DateTime StatusStartedAtUtc { get; set; }

        public int? SessionId { get; set; }
        public DateTime? SessionStartedAtUtc { get; set; }

        public int DurationSeconds { get; set; }
    }
}
