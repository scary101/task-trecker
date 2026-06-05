using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class TeamMemberUsernameDto
    {
        public int MemberId { get; set; }
        public string Username { get; set; } = "";
    }
}
