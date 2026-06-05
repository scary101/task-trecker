using steptreck.Domain.DTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class NotificationsViewModel
    {
        private readonly HttpClient _http;

        public NotificationsViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<NotificationDto>> GetLatestAsync(int take = 30)
        {
            try
            {
                take = Math.Clamp(take, 1, 100);

                return await _http.GetFromJsonAsync<List<NotificationDto>>(
                    $"api/notifications?take={take}"
                ) ?? new List<NotificationDto>();
            }
            catch
            {
                return new List<NotificationDto>();
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<int>(
                    "api/notifications/unread-count"
                );
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> MarkReadAsync(long id)
        {
            try
            {
                var res = await _http.PostAsync(
                    $"api/notifications/{id}/read",
                    content: null
                );

                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
