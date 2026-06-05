using steptreck.Domain;
using steptreck.Domain.DTOs;
using System.Net.Http.Json;

namespace steptreck.Web.ViewModel
{
    public class AuditViewModel
    {
        private readonly HttpClient _httpClient;

        public AuditViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(string? tableName = null)
        {
            var url = string.IsNullOrEmpty(tableName) ? "api/audit" : $"api/audit?tableName={Uri.EscapeDataString(tableName)}";
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Access denied: User does not have admin privileges.");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<AuditLog>>() ?? new List<AuditLog>();
        }

        public Task<PagedResult<AuditLogDto>> GetOrganizationLogsAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? entityType = null,
            string sortBy = "date",
            bool sortDesc = true)
        {
            var query = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
                $"sortBy={Uri.EscapeDataString(sortBy)}",
                $"sortDesc={sortDesc.ToString().ToLowerInvariant()}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search.Trim())}");

            if (!string.IsNullOrWhiteSpace(entityType))
                query.Add($"entityType={Uri.EscapeDataString(entityType.Trim())}");

            return GetAppAuditLogsAsync($"api/audit/org?{string.Join("&", query)}");
        }


        public Task<PagedResult<AuditLogDto>> GetOrganizationLogsAsync(
    int page = 1,
    int pageSize = 20,
    string? search = null,
    string? action = null,
    string? actor = null,
    string? dateFrom = null,
    string? dateTo = null,
    string sortBy = "date",
    bool sortDesc = true)
        {
            var query = BuildQuery(page, pageSize, search, action, actor, dateFrom, dateTo, sortBy, sortDesc);
            return GetAppAuditLogsAsync($"api/audit/org?{query}");
        }

        public Task<PagedResult<AuditLogDto>> GetProjectLogsAsync(
            int projectId,
            int page = 1,
            int pageSize = 20,
            string? action = null,
            string? actor = null,
            string? dateFrom = null,
            string? dateTo = null)
        {
            var query = BuildQuery(page, pageSize, null, action, actor, dateFrom, dateTo, null, true);
            return GetAppAuditLogsAsync($"api/audit/projects/{projectId}?{query}");
        }

        public Task<PagedResult<AuditLogDto>> GetTeamLogsAsync(
            int teamId,
            int page = 1,
            int pageSize = 20,
            string? action = null,
            string? actor = null,
            string? dateFrom = null,
            string? dateTo = null)
        {
            var query = BuildQuery(page, pageSize, null, action, actor, dateFrom, dateTo, null, true);
            return GetAppAuditLogsAsync($"api/audit/teams/{teamId}?{query}");
        }

        public Task<PagedResult<AuditLogDto>> GetMemberLogsAsync(
            int memberId,
            int page = 1,
            int pageSize = 20,
            string? action = null,
            string? actor = null,
            string? dateFrom = null,
            string? dateTo = null)
        {
            var query = BuildQuery(page, pageSize, null, action, actor, dateFrom, dateTo, null, true);
            return GetAppAuditLogsAsync($"api/audit/members/{memberId}?{query}");
        }

        private static string BuildQuery(
            int page,
            int pageSize,
            string? search,
            string? action,
            string? actor,
            string? dateFrom,
            string? dateTo,
            string? sortBy,
            bool sortDesc)
        {
            var query = new List<string>
    {
        $"page={page}",
        $"pageSize={pageSize}"
    };

            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search.Trim())}");

            if (!string.IsNullOrWhiteSpace(action))
                query.Add($"action={Uri.EscapeDataString(action.Trim())}");

            if (!string.IsNullOrWhiteSpace(actor))
                query.Add($"actor={Uri.EscapeDataString(actor.Trim())}");

            if (!string.IsNullOrWhiteSpace(dateFrom))
                query.Add($"dateFrom={Uri.EscapeDataString(dateFrom.Trim())}");

            if (!string.IsNullOrWhiteSpace(dateTo))
                query.Add($"dateTo={Uri.EscapeDataString(dateTo.Trim())}");

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                query.Add($"sortBy={Uri.EscapeDataString(sortBy.Trim())}");
                query.Add($"sortDesc={sortDesc.ToString().ToLowerInvariant()}");
            }

            return string.Join("&", query);
        }

        private async Task<PagedResult<AuditLogDto>> GetAppAuditLogsAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("Access denied: User does not have access to audit logs.");

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>() ?? new PagedResult<AuditLogDto>();
        }
    }
}
