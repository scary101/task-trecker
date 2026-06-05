using steptreck.Domain.DTOs.SubscriptionsDTOs;
using System.Net.Http.Json;

namespace steptreck.Web.Services
{
    public interface ISubscriptionCheckState
    {
        SubCheckInfoDto? Current { get; }
        Task<SubCheckInfoDto?> RefreshAsync(CancellationToken ct = default);
        void Clear();
    }

    public sealed class SubscriptionCheckState : ISubscriptionCheckState
    {
        private readonly HttpClient _httpClient;

        public SubscriptionCheckState(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public SubCheckInfoDto? Current { get; private set; }

        public async Task<SubCheckInfoDto?> RefreshAsync(CancellationToken ct = default)
        {
            try
            {
                Current = await _httpClient.GetFromJsonAsync<SubCheckInfoDto>(
                    "api/subscriptions/check",
                    ct);
            }
            catch
            {
                Current = null;
            }

            return Current;
        }

        public void Clear()
        {
            Current = null;
        }
    }
}
