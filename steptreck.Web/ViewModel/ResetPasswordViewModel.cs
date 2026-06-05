using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Web.Services;
using System.Net.Http;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class ResetPasswordViewModel
    {
        private readonly HttpClient _httpClient;

        public bool LastResponseSuccess { get; private set; } = false;

        public ResetPasswordViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<bool> SendResetLinkAsync(EmailDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/reset-password/resetlink", model);
            LastResponseSuccess = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/reset-password", model);
            LastResponseSuccess = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CheckToken(string token)
        {
            var response = await _httpClient.GetAsync($"api/reset-password/check-reset-token?token={token}");
            LastResponseSuccess = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
        }
    }
}
