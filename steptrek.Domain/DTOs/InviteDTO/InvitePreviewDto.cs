using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs.InviteDTO
{
    public class InvitePreviewDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? CorporateEmail { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
