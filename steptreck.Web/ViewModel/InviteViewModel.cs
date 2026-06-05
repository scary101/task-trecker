using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Domain.DTOs.InviteDTO;
using steptreck.Web.Services;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public class InviteViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly IJwtService _jwt;
        private readonly IUserRoleStore _roleStore;
        private readonly ISubscriptionCheckState _subscriptionCheckState;

        public InviteViewModel(
            HttpClient http,
            IJwtService jwt,
            IUserRoleStore roleStore,
            ISubscriptionCheckState subscriptionCheckState)
        {
            _http = http;
            _jwt = jwt;
            _roleStore = roleStore;
            _subscriptionCheckState = subscriptionCheckState;
        }

        public async Task<ApiResult> InviteMemberAsync(RegisterMebmerDto dto)
        {
            var res = await _http.PostAsJsonAsync("api/invite/invite", dto);
            var payload = await res.Content.ReadFromJsonAsync<ApiMessageDto>(JsonOpts);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, payload?.Message)
                : new ApiResult(false, null, payload?.Error);
        }

        public async Task<ApiResult> AcceptInviteAsync(AcceptInviteDto dto)
        {
            var res = await _http.PostAsJsonAsync("api/invite/accept", dto);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ApiMessageDto>(JsonOpts);
                return new ApiResult(false, null, err?.Error ?? "Ошибка принятия приглашения");
            }

            var payload = await res.Content.ReadFromJsonAsync<AcceptInviteResult>(JsonOpts);

            if (payload?.Success == true && !string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                await _jwt.SetTokenAsync(payload.AccessToken);
                var (roleId, roleName) = ReadRoleFromJwt(payload.AccessToken);
                await _roleStore.SetRoleAsync(roleId, roleName);
                await _subscriptionCheckState.RefreshAsync();
            }

            return payload?.Success == true
                ? new ApiResult(true, payload.Message)
                : new ApiResult(false, null, payload?.Message ?? "Ошибка принятия приглашения");
        }

        private static (int? roleId, string? roleName) ReadRoleFromJwt(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (null, null);

            var parts = token.Split('.');
            if (parts.Length < 2)
                return (null, null);

            try
            {
                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;
                    case 3:
                        payload += "=";
                        break;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int? roleId = null;
                string? roleName = null;

                if (root.TryGetProperty("role_id", out var roleIdProp) &&
                    roleIdProp.ValueKind == JsonValueKind.String &&
                    int.TryParse(roleIdProp.GetString(), out var parsedRoleId))
                {
                    roleId = parsedRoleId;
                }

                if (root.TryGetProperty("role", out var roleNameProp))
                {
                    roleName = roleNameProp.GetString();
                }

                return (roleId, roleName);
            }
            catch
            {
                return (null, null);
            }
        }

    }
}
