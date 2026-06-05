using System.Security.Cryptography;
using System.Text;

namespace steptreck.API.Infrastructure.Secutiry
{
    public class InvationTokenHelper
    {
        private readonly string _pepper;

        public InvationTokenHelper(IConfiguration cfg)
        {
            _pepper = cfg["Invitation:Pepper"]
                ?? throw new ArgumentNullException("Invitation:Pepper not found");
        }

        public string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", ""); 
        }

        public string HashToken(string rawToken)
        {
            using var sha = SHA256.Create();
            var data = Encoding.UTF8.GetBytes(rawToken + _pepper);
            return Convert.ToHexString(sha.ComputeHash(data));
        }
    }
}
