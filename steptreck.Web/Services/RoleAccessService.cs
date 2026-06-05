using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace steptreck.Web.Services
{
    public interface IRoleAccessService
    {
        Task<int?> GetCurrentRoleIdAsync();
        Task<bool> IsAdminAsync();
        Task<bool> CanAccessWorkplacePathAsync(string path);
        string GetDefaultWorkplacePath(int? roleId);
    }

    public class RoleAccessService : IRoleAccessService
    {
        private const int OwnerRoleId = 1;
        private const int TeamLeadRoleId = 3;
        private const int EmployeeRoleId = 4;
        private const int AdminRoleId = 5;
        private const int ProjectManagerRoleId = 6;

        private readonly IUserRoleStore _roleStore;
        private readonly IJwtService _jwtService;

        public RoleAccessService(IUserRoleStore roleStore, IJwtService jwtService)
        {
            _roleStore = roleStore;
            _jwtService = jwtService;
        }

        public async Task<bool> IsAdminAsync()
            => await GetCurrentRoleIdAsync() == AdminRoleId;

        public async Task<int?> GetCurrentRoleIdAsync()
        {
            var token = await _jwtService.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var tokenRoleId = GetRoleIdFromJwt(token);
                if (tokenRoleId.HasValue)
                    return tokenRoleId;

                return GetRoleIdFromName(GetRoleNameFromJwt(token));
            }

            var roleId = await _roleStore.GetRoleIdAsync();
            if (roleId.HasValue)
                return roleId;

            var roleName = await _roleStore.GetRoleNameAsync();
            return GetRoleIdFromName(roleName);
        }

        public async Task<bool> CanAccessWorkplacePathAsync(string path)
        {
            var roleId = await GetCurrentRoleIdAsync();
            if (!roleId.HasValue)
                return false;

            var normalized = NormalizePath(path);
            if (normalized == "workplace")
                return IsAnyAuthorizedRole(roleId.Value);

            if (normalized is "workplace/metrics" or "workplace/data" or "workplace/audit")
                return roleId == AdminRoleId;

            if (normalized is "workplace/owner-dashboard" or "workplace/subscription/manage" or "workplace/projects" or "workplace/members")
                return roleId == OwnerRoleId;

            if (IsMatch(normalized, @"^workplace/projects/\d+($|/(dashboard|members|teams|audit)$)"))
                return roleId == OwnerRoleId;

            if (IsMatch(normalized, @"^workplace/projects/\d+/files$"))
                return roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId;

            if (IsMatch(normalized, @"^workplace/projects/\d+/teams/\d+($|/(dashboard|members|stats|files|calendar|chat|audit)$)"))
                return roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId;

            if (IsMatch(normalized, @"^workplace/projects/\d+/teams/\d+/tasks($|/history$)"))
                return roleId is TeamLeadRoleId or EmployeeRoleId;

            if (normalized is "workplace/chats" or "workplace/notifications" or "workplace/sessions/history" or "workplace/charts/work-time-statistics")
                return roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId;

            if (normalized is "workplace/tasks/my" or "workplace/tasks/my-history")
                return roleId is TeamLeadRoleId or EmployeeRoleId;

            if (normalized is "workplace/sessions/my-history")
                return roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId;

            if (normalized is "workplace/sessions/active")
                return roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId or ProjectManagerRoleId;

            if (IsMatch(normalized, @"^workplace/members/\d+($|/audit$)"))
                return roleId is OwnerRoleId or TeamLeadRoleId;

            if (IsMatch(normalized, @"^workplace/members/\d+/tasks($|/history$)"))
                return roleId is TeamLeadRoleId;

            if (IsMatch(normalized, @"^workplace/tasks/\d+$"))
                return roleId is TeamLeadRoleId or EmployeeRoleId;

            return false;
        }

        public string GetDefaultWorkplacePath(int? roleId)
        {
            return roleId switch
            {
                AdminRoleId => "/workplace/metrics",
                OwnerRoleId => "/workplace/owner-dashboard",
                ProjectManagerRoleId => "/workplace/sessions/active",
                TeamLeadRoleId => "/workplace/tasks/my",
                EmployeeRoleId => "/workplace/tasks/my",
                _ => "/login"
            };
        }

        private static int? GetRoleIdFromJwt(string? token)
        {
            var root = ReadJwtPayload(token);
            if (root is null)
                return null;

            if (root.Value.TryGetProperty("role_id", out var roleIdEl))
            {
                if (roleIdEl.ValueKind == JsonValueKind.Number && roleIdEl.TryGetInt32(out var roleId))
                    return roleId;

                if (roleIdEl.ValueKind == JsonValueKind.String && int.TryParse(roleIdEl.GetString(), out roleId))
                    return roleId;
            }

            return null;
        }

        private static string? GetRoleNameFromJwt(string? token)
        {
            var root = ReadJwtPayload(token);
            if (root is null)
                return null;

            if (root.Value.TryGetProperty("role", out var roleEl))
                return ExtractRole(roleEl);

            const string roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            if (root.Value.TryGetProperty(roleClaim, out var claimEl))
                return ExtractRole(claimEl);

            return null;
        }

        private static JsonElement? ReadJwtPayload(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            try
            {
                var payloadJson = DecodeJwtPart(parts[1]);
                using var doc = JsonDocument.Parse(payloadJson);
                return doc.RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractRole(JsonElement roleEl)
        {
            return roleEl.ValueKind switch
            {
                JsonValueKind.String => roleEl.GetString(),
                JsonValueKind.Array => roleEl.GetArrayLength() > 0 ? roleEl[0].GetString() : null,
                _ => null
            };
        }

        private static int? GetRoleIdFromName(string? roleName)
        {
            return roleName?.Trim().ToLowerInvariant() switch
            {
                "owner" => OwnerRoleId,
                "admin" => AdminRoleId,
                "teamlead" => TeamLeadRoleId,
                "team lead" => TeamLeadRoleId,
                "employee" => EmployeeRoleId,
                "projectmanaget" => ProjectManagerRoleId,
                "project manager" => ProjectManagerRoleId,
                _ => null
            };
        }

        private static bool IsAnyAuthorizedRole(int roleId)
            => roleId is OwnerRoleId or TeamLeadRoleId or EmployeeRoleId or AdminRoleId or ProjectManagerRoleId;

        private static string NormalizePath(string path)
            => path.Split('?', '#')[0].Trim().Trim('/').ToLowerInvariant();

        private static bool IsMatch(string path, string pattern)
            => Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string DecodeJwtPart(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            var bytes = Convert.FromBase64String(s);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
