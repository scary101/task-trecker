using steptreck.Domain.DTOs.MemberDTOs;
using steptreck.Domain.DTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public sealed class MembersVm
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private readonly HttpClient _http;

        public MembersVm(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<MemberDto>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<List<MemberDto>>(
                "api/members"
            ) ?? new List<MemberDto>();
        }

        public async Task<FioInfoDto?> GetMyFioAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<FioInfoDto>(
                    "api/members/me/fio"
                );
            }
            catch
            {
                return null;
            }
        }



        public async Task<MemberProfileDto?> GetMyProfileAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<MemberProfileDto>(
                    "api/members/me"
                );
            }
            catch
            {
                return null;
            }
        }
        public async Task<MemberProfileDto?> GetProfile(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<MemberProfileDto>(
                    $"api/members/profile/{id}"
                );
            }
            catch
            {
                return null;
            }
        }
        public async Task<List<MemberDto>> GetByProject(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<MemberDto>>(
                    $"api/members/project/{id}"
                ) ?? new List<MemberDto>();
            }
            catch
            {
                return new List<MemberDto>();
            }
        }



        public async Task<MemberDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<MemberDto>(
                    $"api/members/{id}"
                );
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateAsync(int id, UpdateMemberDto dto)
        {
            var res = await _http.PutAsJsonAsync(
                $"api/members/{id}", dto
            );

            return res.IsSuccessStatusCode;
        }
        public async Task<ApiResult> UpdateUsernameAsync(string username, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PutAsJsonAsync(
                    "api/members/username",
                    new UpdateUsernameDto { Username = username },
                    ct);

                if (res.IsSuccessStatusCode)
                {
                    var payload = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
                    string? savedUsername = null;
                    payload?.TryGetValue("username", out savedUsername);
                    return new ApiResult(true, savedUsername);
                }

                var msg = await res.Content.ReadFromJsonAsync<ApiMessageDto>(cancellationToken: ct);
                return new ApiResult(false, null, msg?.Error ?? msg?.Message ?? "Не удалось обновить никнейм.");
            }
            catch
            {
                return new ApiResult(false, null, "Ошибка запроса.");
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var res = await _http.DeleteAsync(
                $"api/members/{id}"
            );

            return res.IsSuccessStatusCode;
        }

        public async Task<string?> UploadAvatarAsync(
            Stream fileStream,
            string fileName)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);

            var res = await _http.PostAsync(
                "api/members/avatar", content
            );

            if (!res.IsSuccessStatusCode)
                return null;

            try
            {
                var json = await res.Content
                    .ReadFromJsonAsync<Dictionary<string, string>>();

                if (json is null || !json.TryGetValue("avatarUrl", out var avatarUrl) || string.IsNullOrWhiteSpace(avatarUrl))
                    return null;

                if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
                    return avatarUrl;

                var baseUri = _http.BaseAddress;
                if (baseUri is null)
                    return avatarUrl;

                return new Uri(baseUri, avatarUrl).ToString();
            }
            catch
            {
                return null;
            }
        }
    }

}
