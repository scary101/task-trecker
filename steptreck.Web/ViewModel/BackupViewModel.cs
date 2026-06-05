using steptreck.Domain.DTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public sealed class BackupViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);
        public record DownloadedFile(string FileName, string ContentType, byte[] Content);

        private readonly HttpClient _http;

        public BackupViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<(ApiResult Result, DownloadedFile? File)> DownloadAsync()
        {
            try
            {
                var res = await _http.GetAsync("api/backup/download");
                if (!res.IsSuccessStatusCode)
                {
                    var text = await res.Content.ReadAsStringAsync();
                    return (new ApiResult(false, null, text), null);
                }

                var bytes = await res.Content.ReadAsByteArrayAsync();
                var contentType = res.Content.Headers.ContentType?.ToString()
                                  ?? "application/octet-stream";
                var cd = res.Content.Headers.ContentDisposition;
                var fileName =
                    cd?.FileNameStar?.Trim('"')
                    ?? cd?.FileName?.Trim('"')
                    ?? $"backup_{DateTime.UtcNow:yyyyMMddHHmmss}.sql";

                return (new ApiResult(true), new DownloadedFile(fileName, contentType, bytes));
            }
            catch (Exception ex)
            {
                return (new ApiResult(false, null, $"Ошибка скачивания: {ex.Message}"), null);
            }
        }

        public async Task<ApiResult> UploadAsync(Stream fileStream, string fileName, string? contentType = null)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);
                var safeContentType = string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType;

                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(safeContentType);

                form.Add(fileContent, "file", fileName);

                var res = await _http.PostAsync("api/backup/upload", form);
                var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>();
                return res.IsSuccessStatusCode
                    ? new ApiResult(true, msg?.Message ?? "База восстановлена.")
                    : new ApiResult(false, null, msg?.Error ?? "Не удалось восстановить базу.");
            }
            catch (Exception ex)
            {
                return new ApiResult(false, null, $"Ошибка загрузки: {ex.Message}");
            }
        }
    }
}
