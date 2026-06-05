using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.DTOs.TaskDTOs;
using System.Net.Http.Json;
using System.Text;

namespace steptreck.Web.ViewModel
{
    public class TaskViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private readonly HttpClient _http;

        public TaskViewModel(HttpClient http)
        {
            _http = http;
        }

        private sealed class CreatedIdDto
        {
            public int Id { get; set; }
        }

        public async Task<(ApiResult Result, int? Id)> CreateAsync(CreateTaskForTeamDto dto, CancellationToken ct = default)
        {
            var payload = new
            {
                dto.ProjectId,
                dto.TeamId,
                dto.AssignedToMemberId,
                dto.Title,
                dto.Description,
                dto.Deadline,
                Priority = (int)dto.Priority,
                dto.Checklist
            };

            var res = await _http.PostAsJsonAsync("api/task", payload, ct);

            if (!res.IsSuccessStatusCode)
                return (new ApiResult(false), null);

            var created = await res.Content.ReadFromJsonAsync<CreatedIdDto>(cancellationToken: ct);
            return (new ApiResult(true), created?.Id);
        }

        public async Task<TTask?> GetByIdAsync<TTask>(int taskId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<TTask>($"api/task/{taskId}", ct);
        }

        public async Task<ApiResult> CompleteAsync(int taskId, CancellationToken ct = default)
        {
            var res = await _http.PostAsync($"api/task/{taskId}/complete", content: null, ct);
            return res.IsSuccessStatusCode ? new ApiResult(true) : new ApiResult(false);
        }

        public async Task<ApiResult> DeleteAsync(int taskId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/task/{taskId}", ct);
            return res.IsSuccessStatusCode ? new ApiResult(true) : new ApiResult(false);
        }

        public async Task<List<TTask>> GetTeamTasksAsync<TTask>(int teamId, TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            var url = $"api/task/team/{teamId}{ToQuery(filter)}";
            return await _http.GetFromJsonAsync<List<TTask>>(url, ct) ?? new List<TTask>();
        }

        public async Task<List<TTask>> GetMemberTasksAsync<TTask>(int memberId, TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            var url = $"api/task/member/{memberId}{ToQuery(filter)}";
            return await _http.GetFromJsonAsync<List<TTask>>(url, ct) ?? new List<TTask>();
        }

        public async Task<List<TTask>> GetMyTasksAsync<TTask>(TaskListFilterDto? filter = null, CancellationToken ct = default)
        {
            var url = $"api/task/my{ToQuery(filter)}";
            return await _http.GetFromJsonAsync<List<TTask>>(url, ct) ?? new List<TTask>();
        }
        public async Task<bool> UpdateDeadLine(PutDeadLineDto dto)
        {
            var res = await _http.PutAsJsonAsync(
                $"api/task/deadline", dto
            );

            return res.IsSuccessStatusCode;
        }

        private static string ToQuery(TaskListFilterDto? f)
        {
            if (f is null) return "";

            var sb = new StringBuilder();
            void Add(string name, object? value)
            {
                if (value is null) return;
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.Append(Uri.EscapeDataString(name));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(value.ToString()!));
            }

            Add("status", f.Status);
            Add("date-to", f.DateTo);
            Add("date-from", f.DateFrom);

            return sb.ToString();
        }
    }
}
