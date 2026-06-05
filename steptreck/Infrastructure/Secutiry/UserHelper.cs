using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;
using System.Security.Claims;

namespace steptreck.API.Infrastructure.Secutiry
{
    public class UserHelper
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserHelper(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            int currentUserId = GetCurrentUserId();
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
        }

        public int GetCurrentUserId()
        {
            var http = _httpContextAccessor.HttpContext;

            var authHeader = http?.Request.Headers["Authorization"].ToString();

            var userIdClaim = http?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException(
                    $"Invalid token. AuthHeader: {authHeader}"
                );

            return userId;
        }
        public async Task<int> GetCurrentMemberId(CancellationToken ct = default)
        {
            int orgId = GetCurrentOrganizationId();
            int userId = GetCurrentUserId();

            var memberId = await _context.Members
                .Where(m => m.OrganizationId == orgId && m.UserId == userId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(ct);

            if (memberId == 0)
                throw new UnauthorizedAccessException("Member not found for current user/org.");

            return memberId;
        }


        public int GetCurrentOrganizationId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            var claims = user?.Claims?.Select(c => $"{c.Type}={c.Value}").ToList() ?? new();
            Console.WriteLine("JWT CLAIMS: " + string.Join(", ", claims));

            var orgIdClaim = user?.FindFirst("org_id")?.Value;

            if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out int orgId))
                throw new UnauthorizedAccessException("Invalid token (org_id missing)");

            return orgId;
        }


        public string GetCurrentRole()
        {
            var role = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
                throw new UnauthorizedAccessException("Invalid token (role missing)");

            return role;
        }
        public string GetFullNameMember(Member member)
        {
            return string.Join(" ",
                new[] { member.Surname, member.Name, member.Patronymic }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}
