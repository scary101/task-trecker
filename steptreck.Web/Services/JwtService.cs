using Blazored.LocalStorage;
using System.Text.Json;

namespace steptreck.Web.Services
{
    public interface IJwtService
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
    }

    public class JwtService : IJwtService
    {
        private readonly ILocalStorageService _localStorage;
        private const string TokenKey = "jwt";

        public JwtService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SetTokenAsync(string token)
        {
            var normalized = ExtractAccessToken(token);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            await _localStorage.SetItemAsync(TokenKey, normalized);
        }

        public async Task<string?> GetTokenAsync()
        {
            var stored = await _localStorage.GetItemAsync<string>(TokenKey);
            var normalized = ExtractAccessToken(stored);

            if (!string.Equals(stored, normalized, StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(normalized))
                    await _localStorage.RemoveItemAsync(TokenKey);
                else
                    await _localStorage.SetItemAsync(TokenKey, normalized);
            }

            return normalized;
        }

        public async Task RemoveTokenAsync()
        {
            await _localStorage.RemoveItemAsync(TokenKey);
        }

        private static string? ExtractAccessToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var raw = token.Trim();

            if (raw.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("accessToken", out var accessTokenProp) ||
                        doc.RootElement.TryGetProperty("AccessToken", out accessTokenProp))
                    {
                        var accessToken = accessTokenProp.GetString();
                        if (!string.IsNullOrWhiteSpace(accessToken))
                            return accessToken.Trim();
                    }
                }
                catch
                {
                    return raw;
                }
            }

            return raw;
        }
    }
}
