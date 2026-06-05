using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.InviteDTO
{
    public class AcceptInviteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int? OrganizationId { get; set; }
        public string? AccessToken { get; set; }
    }

}
