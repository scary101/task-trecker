using Microsoft.JSInterop;

namespace steptreck.Web.Services
{
    public interface IInviteTokenStore
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task ClearTokenAsync();
    }

    public class InviteTokenStore : IInviteTokenStore
    {
        private const string TokenKey = "invite_token";
        private readonly IJSRuntime _js;

        public InviteTokenStore(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            await _js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        }

        public async Task ClearTokenAsync()
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        }
    }
}
