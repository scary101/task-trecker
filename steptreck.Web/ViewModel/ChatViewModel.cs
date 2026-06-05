using System.Net.Http.Json;
using System.Text.Json;
using steptreck.Domain.DTOs.ChatDTOs;
using steptreck.Domain.DTOs.MemberDTOs;

namespace steptreck.Web.ViewModel
{
    public class ChatViewModel
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ChatViewModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(int teamId, long? beforeId, int take, CancellationToken ct = default)
        {
            var url = $"api/team-chats/{teamId}/messages?take={take}";
            if (beforeId.HasValue)
                url += $"&beforeId={beforeId.Value}";

            try
            {
                return await _http.GetFromJsonAsync<List<ChatMessageDto>>(url, JsonOpts, ct)
                    ?? new List<ChatMessageDto>();
            }
            catch
            {
                return new List<ChatMessageDto>();
            }
        }

        public async Task<List<ChatListItemDto>> GetChatsAsync(CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<ChatListItemDto>>(
                    "api/team-chats/chats",
                    JsonOpts,
                    ct) ?? new List<ChatListItemDto>();
            }
            catch
            {
                return new List<ChatListItemDto>();
            }
        }

        public async Task<List<TeamMemberUsernameDto>> GetTeamUsernamesAsync(int teamId, CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<TeamMemberUsernameDto>>(
                    $"api/team-chats/{teamId}/usernames",
                    JsonOpts,
                    ct) ?? new List<TeamMemberUsernameDto>();
            }
            catch
            {
                return new List<TeamMemberUsernameDto>();
            }
        }
    }
}
