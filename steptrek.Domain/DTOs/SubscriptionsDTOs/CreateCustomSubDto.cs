using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.SubscriptionsDTOs
{
    public class CreateCustomSubDto
    {
        public int MebmersCount { get; set; }
        public int TeamsCount { get; set; }
        public int ProjectsCount { get; set; }
        public int MonthCount { get; set; }

    }
}
