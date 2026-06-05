using steptreck.Domain.DTOs;
using System.Net;
using System.Net.Http.Json;
using steptreck.Domain.DTOs.WorkSessionDTOs;

namespace steptreck.Web.ViewModel
{
    public sealed class WorkSessionViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private readonly HttpClient _http;

        public WorkSessionViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<IReadOnlyList<ActiveEmployeeSessionDto>> GetActiveSessionsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ActiveEmployeeSessionDto>>("api/work-sessions/active")
                       ?? new List<ActiveEmployeeSessionDto>();
            }
            catch
            {
                return Array.Empty<ActiveEmployeeSessionDto>();
            }
        }

        public async Task<IReadOnlyList<WorkSessionHistoryDto>> GetCompletedSessionsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<WorkSessionHistoryDto>>("api/work-sessions/history")
                       ?? new List<WorkSessionHistoryDto>();
            }
            catch
            {
                return Array.Empty<WorkSessionHistoryDto>();
            }
        }

        public async Task<IReadOnlyList<ActiveEmployeeSessionDto>> GetProjectActiveSessionsAsync(int projectId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ActiveEmployeeSessionDto>>($"api/work-sessions/active/project/{projectId}")
                       ?? new List<ActiveEmployeeSessionDto>();
            }
            catch
            {
                return Array.Empty<ActiveEmployeeSessionDto>();
            }
        }

        public async Task<IReadOnlyList<WorkSessionHistoryDto>> GetProjectCompletedSessionsAsync(int projectId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<WorkSessionHistoryDto>>($"api/work-sessions/history/project/{projectId}")
                       ?? new List<WorkSessionHistoryDto>();
            }
            catch
            {
                return Array.Empty<WorkSessionHistoryDto>();
            }
        }

        public async Task<IReadOnlyList<ActiveEmployeeSessionDto>> GetTeamActiveSessionsAsync(int teamId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ActiveEmployeeSessionDto>>($"api/work-sessions/active/team/{teamId}")
                       ?? new List<ActiveEmployeeSessionDto>();
            }
            catch
            {
                return Array.Empty<ActiveEmployeeSessionDto>();
            }
        }

        public async Task<IReadOnlyList<WorkSessionHistoryDto>> GetTeamCompletedSessionsAsync(int teamId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<WorkSessionHistoryDto>>($"api/work-sessions/history/team/{teamId}")
                       ?? new List<WorkSessionHistoryDto>();
            }
            catch
            {
                return Array.Empty<WorkSessionHistoryDto>();
            }
        }

        public async Task<UserCurrentSessionDto?> GetUserCurrentOrLastSessionAsync(int userId)
        {
            try
            {
                var response = await _http.GetAsync($"api/work-sessions/profile/{userId}/current");
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<UserCurrentSessionDto>();
            }
            catch
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<WorkSessionHistoryDto>> GetUserSessionHistoryAsync(int userId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<WorkSessionHistoryDto>>($"api/work-sessions/history/{userId}")
                       ?? new List<WorkSessionHistoryDto>();
            }
            catch
            {
                return Array.Empty<WorkSessionHistoryDto>();
            }
        }

        public async Task<IReadOnlyList<WorkSessionHistoryDto>> GetMySessionHistoryAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<WorkSessionHistoryDto>>("api/work-sessions/my-history")
                       ?? new List<WorkSessionHistoryDto>();
            }
            catch
            {
                return Array.Empty<WorkSessionHistoryDto>();
            }
        }

        public async Task<ApiResult> StartSessionAsync()
        {
            return await PostCommandAsync("api/work-sessions/start", "Не удалось запустить сессию.");
        }

        public async Task<ApiResult> ToggleSessionAsync()
        {
            return await PostCommandAsync("api/work-sessions/toggle", "Не удалось переключить состояние сессии.");
        }

        public async Task<ApiResult> StopSessionAsync()
        {
            return await PostCommandAsync("api/work-sessions/stop", "Не удалось остановить сессию.");
        }

        private async Task<ApiResult> PostCommandAsync(string url, string fallbackError)
        {
            try
            {
                var response = await _http.PostAsync(url, null);
                var payload = await response.Content.ReadFromJsonAsync<ApiMessageDto>();

                return response.IsSuccessStatusCode
                    ? new ApiResult(true, payload?.Message)
                    : new ApiResult(false, null, payload?.Error ?? payload?.Message ?? fallbackError);
            }
            catch
            {
                return new ApiResult(false, null, "Ошибка запроса.");
            }
        }
    }
}
