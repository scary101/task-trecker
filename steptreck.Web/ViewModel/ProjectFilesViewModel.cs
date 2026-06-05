using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ProjectDTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public class ProjectFilesViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ProjectFilesViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProjectFileReadDto>> GetFilesAsync(int projectId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<List<ProjectFileReadDto>>(
                $"api/ProjectFiles/{projectId}/files",
                JsonOpts,
                ct) ?? new List<ProjectFileReadDto>();
        }

        public async Task<(ApiResult Result, ProjectFileReadDto? File)> UploadAsync(
            int projectId,
            Stream fileStream,
            string fileName,
            string? contentType,
            CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            if (!string.IsNullOrWhiteSpace(contentType))
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            content.Add(fileContent, "File", fileName);

            var res = await _http.PostAsync(
                $"api/ProjectFiles/{projectId}/files",
                content,
                ct);

            if (res.IsSuccessStatusCode)
            {
                var dto = await res.Content.ReadFromJsonAsync<ProjectFileReadDto>(JsonOpts, ct);
                return (new ApiResult(true), dto);
            }

            var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>(JsonOpts, ct);
            return (new ApiResult(false, null, msg?.Message), null);
        }
        public async Task<(byte[] Data, string FileName, string ContentType)?> DownloadAsync(
            int attachmentId,
            CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"api/ProjectFiles/files/{attachmentId}/download", ct);
            if (!res.IsSuccessStatusCode)
                return null;

            var data = await res.Content.ReadAsByteArrayAsync(ct);
            var contentType = res.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = "file";
            var cd = res.Content.Headers.ContentDisposition;

            if (!string.IsNullOrWhiteSpace(cd?.FileNameStar))
                fileName = cd.FileNameStar;
            else if (!string.IsNullOrWhiteSpace(cd?.FileName))
                fileName = cd.FileName.Trim('"');

            return (data, fileName, contentType);
        }

        public async Task<ApiResult> DeleteAsync(int attachmentId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync(
                $"api/ProjectFiles/files/{attachmentId}",
                ct);

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>(JsonOpts, ct);
            return new ApiResult(false, null, msg?.Message);
        }

    }
}
