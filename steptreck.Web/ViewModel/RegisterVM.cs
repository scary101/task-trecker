using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Web.Services;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class RegisterViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);

        private readonly HttpClient _httpClient;
        private readonly IJwtService _jwtService;

        public RegisterViewModel(HttpClient httpClient, IJwtService jwtService)
        {
            _httpClient = httpClient;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Регистрация организации и владельца: POST api/register/organization
        /// </summary>
        public async Task<ApiResult> RegisterOrganizationAsync(RegisterOrgDTO model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/register/organization", model);
            return await ReadResultAsync(response);
        }

        /// <summary>
        /// Регистрация пользователя: POST api/register/register
        /// </summary>
        public async Task<ApiResult> RegisterUserAsync(RegisterDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/register/register", model);
            return await ReadResultAsync(response);
        }

        private static async Task<ApiResult> ReadResultAsync(HttpResponseMessage response)
        {
            ApiMessageDto? payload = null;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<ApiMessageDto>();
            }
            catch
            {
            }

            if (response.IsSuccessStatusCode)
            {
                return new ApiResult(true, payload?.Message ?? "OK", null);
            }

            if (payload is null)
            {
                var raw = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(raw))
                    return new ApiResult(false, null, raw);
            }

            return new ApiResult(false, null, payload?.Error ?? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
    }
}
