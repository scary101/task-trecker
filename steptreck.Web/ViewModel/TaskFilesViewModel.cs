using steptreck.Domain.DTOs.TeamDTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public sealed class TaskFilesViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);
        public record DownloadedFile(string FileName, string ContentType, byte[] Content);

        private readonly HttpClient _http;

        public TaskFilesViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<(ApiResult Result, TeamFileReadDto? File)> UploadAsync(
            int taskId,
            Stream fileStream,
            string fileName,
            string? contentType = null,
            CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();

            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(contentType ?? "application/octet-stream");

            form.Add(fileContent, "file", fileName);

            var res = await _http.PostAsync($"api/task-files/tasks/{taskId}", form, ct);

            if (!res.IsSuccessStatusCode)
                return (new ApiResult(false), null);

            var dto = await res.Content.ReadFromJsonAsync<TeamFileReadDto>(cancellationToken: ct);
            return (new ApiResult(true), dto);
        }

        public async Task<(ApiResult Result, DownloadedFile? File)> DownloadAsync(
            int attachmentId,
            CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"api/task-files/{attachmentId}/download", ct);

            if (!res.IsSuccessStatusCode)
                return (new ApiResult(false), null);

            var bytes = await res.Content.ReadAsByteArrayAsync(ct);
            var contentType = res.Content.Headers.ContentType?.ToString()
                              ?? "application/octet-stream";

            var cd = res.Content.Headers.ContentDisposition;
            var fileName =
                cd?.FileNameStar?.Trim('"')
                ?? cd?.FileName?.Trim('"')
                ?? $"file_{attachmentId}";

            return (new ApiResult(true),
                new DownloadedFile(fileName, contentType, bytes));
        }

        public async Task<ApiResult> DeleteAsync(int attachmentId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/task-files/{attachmentId}", ct);
            return res.IsSuccessStatusCode
                ? new ApiResult(true)
                : new ApiResult(false);
        }

        public async Task<List<TeamFileReadDto>> GetFilesAsync(int taskId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<List<TeamFileReadDto>>(
                $"api/task-files/tasks/{taskId}",
                ct) ?? new List<TeamFileReadDto>();
        }
    }
}
