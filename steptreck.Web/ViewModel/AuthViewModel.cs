using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Domain.DTOs.PlanDTOs;
using steptreck.Web.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace steptreck.Web.ViewModel
{
    public class AuthViewModel
    {
        public record ApiResult(bool Success, string? Message = null, string? Error = null);
        public record ChallengeResult(bool Success, Guid? ChallengeId = null, int ExpiresInSeconds = 0, string? Message = null, string? Error = null);

        private readonly HttpClient _http;
        private readonly IJwtService _jwt;
        private readonly IUserRoleStore _roleStore;
        private readonly ISubscriptionCheckState _subscriptionCheckState;

        public AuthViewModel(
            HttpClient http,
            IJwtService jwt,
            IUserRoleStore roleStore,
            ISubscriptionCheckState subscriptionCheckState)
        {
            _http = http;
            _jwt = jwt;
            _roleStore = roleStore;
            _subscriptionCheckState = subscriptionCheckState;
        }

        public async Task<ApiResult> SendLoginCodeAsync(LoginDTO dto)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login-code", dto);

            if (res.IsSuccessStatusCode)
            {
                var payload = await ReadApiMessageAsync(res);
                return new ApiResult(true, payload?.Message);
            }

            var errorPayload = await ReadApiMessageAsync(res);
            return new ApiResult(false, null, errorPayload?.Error ?? errorPayload?.Message);
        }

        public async Task<ApiResult> VerifyCodeAsync(VerifyCodeDto dto)
        {
            var res = await _http.PostAsJsonAsync("api/auth/verify-code", dto);

            if (res.IsSuccessStatusCode)
            {
                var tokenJson = await res.Content.ReadAsStringAsync();

                var pay = JsonSerializer.Deserialize<AuthTokenResponse>(
                    tokenJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                await _jwt.SetTokenAsync(pay?.AccessToken ?? tokenJson);
                await _roleStore.SetRoleAsync(pay?.RoleId, pay?.RoleName);
                await _subscriptionCheckState.RefreshAsync();

                return new ApiResult(true);
            }

            var payload = await ReadApiMessageAsync(res);
            return new ApiResult(false, null, payload?.Error);
        }

        public async Task<ChallengeResult> StartLoginNotificationAsync(LoginDTO dto)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login-notification", dto);

            if (res.IsSuccessStatusCode)
            {
                var challenge = await res.Content.ReadFromJsonAsync<LoginChallengeCreatedDto>();
                return new ChallengeResult(
                    true,
                    challenge?.ChallengeId,
                    challenge?.ExpiresInSeconds ?? 120,
                    "Подтвердите вход на телефоне.");
            }

            var payload = await ReadApiMessageAsync(res);
            return new ChallengeResult(false, null, 0, null, payload?.Error ?? payload?.Message);
        }

        public async Task<ApiResult> WaitForLoginApprovalAsync(Guid challengeId, CancellationToken ct)
        {
            var hubUrl = new Uri(_http.BaseAddress!, "/hubs/auth");
            await using var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            var tcs = new TaskCompletionSource<AuthTokenResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.On<AuthTokenResponse>("login-approved", token =>
            {
                if (!string.IsNullOrWhiteSpace(token.AccessToken))
                    tcs.TrySetResult(token);
            });

            await connection.StartAsync(ct);
            await connection.InvokeAsync("JoinLoginChallenge", challengeId, ct);

            using var pollingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
            var pollingTask = PollLoginChallengeTokenAsync(challengeId, pollingCts.Token);
            var completedTask = await Task.WhenAny(tcs.Task, pollingTask);

            pollingCts.Cancel();
            var tokenResponse = await completedTask;

            await _jwt.SetTokenAsync(tokenResponse.AccessToken);
            await _roleStore.SetRoleAsync(tokenResponse.RoleId, tokenResponse.RoleName);
            await _subscriptionCheckState.RefreshAsync();

            return new ApiResult(true);
        }

        private async Task<AuthTokenResponse> PollLoginChallengeTokenAsync(Guid challengeId, CancellationToken ct)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);

                var res = await _http.GetAsync($"api/auth/login-challenges/{challengeId}/token", ct);

                if (res.StatusCode == HttpStatusCode.Accepted)
                    continue;

                if (res.IsSuccessStatusCode)
                {
                    var token = await res.Content.ReadFromJsonAsync<AuthTokenResponse>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        ct);

                    if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                        throw new InvalidOperationException("Не удалось получить токен входа.");

                    return token;
                }

                var payload = await ReadApiMessageAsync(res);
                throw new InvalidOperationException(payload?.Error ?? payload?.Message ?? "Не удалось подтвердить вход.");
            }
        }

        private static async Task<ApiMessageDto?> ReadApiMessageAsync(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ApiMessageDto>(
                    raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return new ApiMessageDto { Error = raw, Message = raw };
            }
        }
        public async Task<bool> IsMemberAsync()
        {
            var token = await _jwt.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/is-member");
            var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode) return false;

            var dto = await res.Content.ReadFromJsonAsync<IsMemberResponse>();
            return dto?.IsMember ?? false;
        }



        public async Task LogoutAsync()
        {
            await _jwt.RemoveTokenAsync();
            await _roleStore.ClearRoleAsync();
            _subscriptionCheckState.Clear();
        }
    }
}
