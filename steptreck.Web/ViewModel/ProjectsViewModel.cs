using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ProjectDTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public class ProjectsViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ProjectsViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<(ApiResult Result, ProjectReadDto? Project)> CreateAsync(ProjectCreateDto dto, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/projects", dto, ct);

            if (res.IsSuccessStatusCode)
            {
                var created = await res.Content.ReadFromJsonAsync<ProjectReadDto>(JsonOpts, ct);
                return (new ApiResult(true), created);
            }

            var msg = await ReadApiMessageAsync(res, ct);
            return (new ApiResult(false, null, msg?.Message), null);
        }

        public async Task<(ApiResult Result, ProjectReadDto? Project)> UpdateAsync(int id, ProjectUpdateDto dto, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"api/projects/{id}", dto, ct);

            if (res.IsSuccessStatusCode)
            {
                var updated = await res.Content.ReadFromJsonAsync<ProjectReadDto>(JsonOpts, ct);
                return (new ApiResult(true), updated);
            }

            var msg = await ReadApiMessageAsync(res, ct);
            return (new ApiResult(false, null, msg?.Message), null);
        }

        public async Task<List<ProjectReadDto>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<List<ProjectReadDto>>(
                       $"api/projects?includeArchived={includeArchived}",
                       JsonOpts,
                       ct)
                   ?? new List<ProjectReadDto>();
        }

        public async Task<(ApiResult Result, ProjectReadDto? Project)> GetByIdAsync(int id, bool includeArchived = false, CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"api/projects/{id}?includeArchived={includeArchived}", ct);

            if (res.IsSuccessStatusCode)
            {
                var project = await res.Content.ReadFromJsonAsync<ProjectReadDto>(JsonOpts, ct);
                return (new ApiResult(true), project);
            }

            var msg = await ReadApiMessageAsync(res, ct);
            return (new ApiResult(false, null, msg?.Message), null);
        }

        public async Task<ApiResult> ToggleArchiveAsync(int id, CancellationToken ct = default)
        {
            var res = await _http.PostAsync($"api/projects/{id}/toggle-archive", content: null, ct);

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await ReadApiMessageAsync(res, ct);
            return new ApiResult(false, null, msg?.Message);
        }

        public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/projects/{id}", ct);

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await ReadApiMessageAsync(res, ct);
            return new ApiResult(false, null, msg?.Error ?? msg?.Message);
        }

        public async Task<(ApiResult Result, string? Url)> UploadBackgroundAsync(
            int projectId,
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

            var res = await _http.PostAsync($"api/projects/{projectId}/background", content, ct);
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

        private sealed class UploadBackgroundResponse
        {
            public string? Url { get; set; }
        }
    }
}
