using steptreck.Domain.DTOs;
using System.Net;
using System.Text.Json;

public class TurnstileService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public TurnstileService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<bool> VerifyAsync(string token, string? remoteIp = null)
    {
        if (_config.GetValue<bool>("Turnstile:Bypass"))
            return string.Equals(token, "dev-turnstile-bypass", StringComparison.Ordinal);

        var secretKey = _config["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(token))
            return false;

        var values = new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp) && IsPublicIp(remoteIp))
            values["remoteip"] = remoteIp;

        var response = await _httpClient.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            new FormUrlEncodedContent(values)
        );

        if (!response.IsSuccessStatusCode)
            return false;

        var raw = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TurnstileResponse>(raw);

        return result?.Success == true;
    }

    private static bool IsPublicIp(string remoteIp)
    {
        if (!IPAddress.TryParse(remoteIp, out var ip))
            return false;

        if (IPAddress.IsLoopback(ip))
            return false;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] != 10 &&
                   !(bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 169 && bytes[1] == 254);
        }

        return !ip.IsIPv6LinkLocal &&
               !ip.IsIPv6SiteLocal &&
               !ip.IsIPv6Multicast;
    }
}
