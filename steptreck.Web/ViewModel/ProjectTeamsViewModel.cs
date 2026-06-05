using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.DTOs.TeamDTOs;
using steptreck.Domain.DTOs.TeamDTOs.MemberDTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public class ProjectTeamsViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);
        private const string DuplicateTeamNameMessage = "Команда с таким названием уже существует в этом проекте.";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ProjectTeamsViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<(ApiResult Result, TeamReadDto? Team)> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/project-teams", dto, ct);
            if (res.IsSuccessStatusCode)
            {
                var created = await res.Content.ReadFromJsonAsync<TeamReadDto>(JsonOpts, ct);
                return (new ApiResult(true), created);
            }

            var msg = await ReadApiMessageAsync(res, ct);
            return (new ApiResult(false, null, NormalizeTeamError(msg?.Error ?? msg?.Message)), null);
        }

        public async Task<ApiResult> AddLeadAsync(CreateLeadMebmerProjectDto dto, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/project-teams/lead", dto, ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }
        public async Task<List<MemberDto>> GetFreeLeads(int projectId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<MemberDto>>(
                    $"api/project-teams/leads-free/{projectId}"
                ) ?? new List<MemberDto>();
            }
            catch
            {
                return new List<MemberDto>();
            }
        }
        public async Task<List<MemberDto>> GetFreeEmployee(int projectId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<MemberDto>>(
                    $"api/project-teams/free/{projectId}"
                ) ?? new List<MemberDto>();
            }
            catch
            {
                return new List<MemberDto>();
            }
        }

        public async Task<ApiResult> AddMemberAsync(CreateMebmerProjectDto dto, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/project-teams/members", dto, ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<ApiResult> UpdateMemberRoleAsync(UpdateTeamRoleDto dto, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync("api/project-teams/members/role", dto, ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<ApiResult> DeleteMemberAsync(int memberId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/project-teams/members/{memberId}", ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<ApiResult> DeleteMemberAsync(int teamId, int memberId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/project-teams/{teamId}/members/{memberId}", ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<ApiResult> DeleteTeamAsync(int teamId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/project-teams/{teamId}", ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<List<TeamReadDto>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<List<TeamReadDto>>(
                $"api/project-teams/project/{projectId}",
                JsonOpts,
                ct) ?? new List<TeamReadDto>();
        }

        public async Task<ApiResult> UpdateTeamAsync(int teamId, UpdateTeamDto dto, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"api/project-teams/{teamId}", dto, ct);
            var msg = await ReadApiMessageAsync(res, ct);

            return res.IsSuccessStatusCode
                ? new ApiResult(true, msg?.Message)
                : new ApiResult(false, null, NormalizeTeamError(msg?.Error ?? msg?.Message));
        }

        public async Task<TeamReadDto?> GetTeamAsync(int teamId, CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<TeamReadDto>(
                    $"api/project-teams/{teamId}",
                    JsonOpts,
                    ct);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<TeamMemberDto>> GetTeamMembersAsync(int teamId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<List<TeamMemberDto>>(
                $"api/project-teams/{teamId}/members",
                JsonOpts,
                ct) ?? new List<TeamMemberDto>();
        }

        public async Task<(ApiResult Result, string? Url)> UploadBackgroundAsync(
            int teamId,
            Stream fileStream,
            string fileName,
            string? contentType = null,
            CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            if (!string.IsNullOrWhiteSpace(contentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            content.Add(fileContent, "file", fileName);

            var res = await _http.PostAsync($"api/project-teams/{teamId}/background", content, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await ReadApiMessageAsync(res, ct);
                return (new ApiResult(false, null, msg?.Error ?? msg?.Message ?? "Не удалось загрузить фон."), null);
            }

            var payload = await res.Content.ReadFromJsonAsync<UploadBackgroundResponse>(JsonOpts, ct);
            return (new ApiResult(true), payload?.Url);
        }

        private static async Task<ApiMessageDto?> ReadApiMessageAsync(HttpResponseMessage response, CancellationToken ct = default)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ApiMessageDto>(raw, JsonOpts);
            }
            catch (JsonException)
            {
                return new ApiMessageDto { Message = raw, Error = raw };
            }
        }

        private static string? NormalizeTeamError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return error;

            if (error.Contains("ux_project_teams_project_name", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("таким именем уже", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("таким названием уже", StringComparison.OrdinalIgnoreCase))
                return DuplicateTeamNameMessage;

            return error;
        }

        private sealed class UploadBackgroundResponse
        {
            public string? Url { get; set; }
        }
    }
}
