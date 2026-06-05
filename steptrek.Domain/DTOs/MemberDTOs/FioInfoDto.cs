using steptreck.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class FioInfoDto
    {
        public required string FullName { get; set; }
        public required string AvatarUrl { get; set; }

    }
}
