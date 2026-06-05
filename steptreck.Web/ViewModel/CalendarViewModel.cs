using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.CalendarDTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public sealed class CalendarViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        public sealed class CalendarEventWriteResultDto
        {
            public int Id { get; set; }
            public int TeamId { get; set; }
            public int? TaskId { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime StartAt { get; set; }
            public DateTime? EndAt { get; set; }
            public string? Description { get; set; }
            public bool IsPinned { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public short TypeId { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public CalendarViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CalendarEventDto>> GetTeamEventsAsync(int teamId, CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CalendarEventDto>>(
                    $"api/calendar/team/{teamId}",
                    JsonOpts,
                    ct) ?? new List<CalendarEventDto>();
            }
            catch
            {
                return new List<CalendarEventDto>();
            }
        }

        public async Task<(ApiResult Result, CalendarEventWriteResultDto? Event)> CreateEventAsync(
            CreateProjectCalendarEventDto dto,
            CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("api/calendar", dto, JsonOpts, ct);

            if (res.IsSuccessStatusCode)
            {
                var created = await res.Content.ReadFromJsonAsync<CalendarEventWriteResultDto>(JsonOpts, ct);
                return (new ApiResult(true), created);
            }

            return (await ReadErrorAsync(res, ct), null);
        }

        public async Task<(ApiResult Result, CalendarEventWriteResultDto? Event)> UpdateEventAsync(
            int eventId,
            UpdateProjectCalendarEventDto dto,
            CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"api/calendar/{eventId}", dto, JsonOpts, ct);

            if (res.IsSuccessStatusCode)
            {
                var updated = await res.Content.ReadFromJsonAsync<CalendarEventWriteResultDto>(JsonOpts, ct);
                return (new ApiResult(true), updated);
            }

            return (await ReadErrorAsync(res, ct), null);
        }

        public async Task<ApiResult> DeleteEventAsync(int eventId, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"api/calendar/{eventId}", ct);

            if (res.IsSuccessStatusCode)
                return new ApiResult(true);

            return await ReadErrorAsync(res, ct);
        }

        private static async Task<ApiResult> ReadErrorAsync(HttpResponseMessage res, CancellationToken ct)
        {
            try
            {
                var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>(JsonOpts, ct);
                return new ApiResult(false, null, msg?.Error ?? msg?.Message ?? res.ReasonPhrase);
            }
            catch
            {
                var text = await res.Content.ReadAsStringAsync(ct);
                return new ApiResult(false, null, string.IsNullOrWhiteSpace(text) ? res.ReasonPhrase : text);
            }
        }
    }
}
