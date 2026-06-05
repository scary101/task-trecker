using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public class RegisterPushTokenRequest
    {
        public string Token { get; set; } = "";
        public string? Platform { get; set; }
    }
}
