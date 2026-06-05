using Microsoft.IdentityModel.Tokens;
using steptreck.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace steptreck.API.Infrastructure.Secutiry
{
    public class TokenHelper
    {
        private string _secretkey;
        private readonly int _tokenExpirationMinutes;

        public TokenHelper(IConfiguration configuration)
        {
            _secretkey = configuration["JwtSettings:SecretKey"]
                    ?? throw new ArgumentNullException("SecretKey not found in configuration");

            _tokenExpirationMinutes = int.Parse(configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "360");
        }

            public string GenerateAuthToken(User user)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.CorporateEmail),
        };

                return BuildToken(claims);
            }

            public string GenerateOrgToken(User user, Member member)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.CorporateEmail),
            new Claim("org_id", member.OrganizationId.ToString()),
            new Claim(ClaimTypes.Role, member.Role.Name),
            new Claim("role_id", member.RoleId.ToString()),
            new Claim("member_id", member.Id.ToString()),
        };

                return BuildToken(claims);
            }

        private string BuildToken(List<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretkey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims, "jwt"),
                Expires = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256),
                Audience = "Client",
                Issuer = "Server"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }




        public string GeneratePasswordResetToken()
        {

            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
