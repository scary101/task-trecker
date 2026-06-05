using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.MemberDTOs
{
    public class MemberDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? AvatarUtl { get; set; }
    }

}
