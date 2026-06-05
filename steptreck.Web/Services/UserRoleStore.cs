using Blazored.LocalStorage;

namespace steptreck.Web.Services
{
    public interface IUserRoleStore
    {
        Task SetRoleAsync(int? roleId, string? roleName);
        Task<int?> GetRoleIdAsync();
        Task<string?> GetRoleNameAsync();
        Task ClearRoleAsync();
    }

    public class UserRoleStore : IUserRoleStore
    {
        private readonly ILocalStorageService _localStorage;
        private const string RoleIdKey = "role_id";
        private const string RoleNameKey = "role_name";

        public UserRoleStore(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SetRoleAsync(int? roleId, string? roleName)
        {
            if (roleId.HasValue)
            {
                await _localStorage.SetItemAsync(RoleIdKey, roleId.Value);
            }
            else
            {
                await _localStorage.RemoveItemAsync(RoleIdKey);
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                await _localStorage.SetItemAsync(RoleNameKey, roleName);
            }
            else
            {
                await _localStorage.RemoveItemAsync(RoleNameKey);
            }
        }

        public async Task<int?> GetRoleIdAsync()
        {
            return await _localStorage.GetItemAsync<int?>(RoleIdKey);
        }

        public async Task<string?> GetRoleNameAsync()
        {
            return await _localStorage.GetItemAsync<string>(RoleNameKey);
        }

        public async Task ClearRoleAsync()
        {
            await _localStorage.RemoveItemAsync(RoleIdKey);
            await _localStorage.RemoveItemAsync(RoleNameKey);
        }
    }
}
