using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public sealed class ApiMessageDto
    {
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
