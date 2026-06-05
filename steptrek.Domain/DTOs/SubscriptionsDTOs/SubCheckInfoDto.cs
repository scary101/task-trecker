using steptreck.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.SubscriptionsDTOs
{
    public class SubCheckInfoDto
    {
        public SubStatus Status { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
