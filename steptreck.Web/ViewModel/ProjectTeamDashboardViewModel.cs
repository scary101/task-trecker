using System.Net.Http.Json;
using steptreck.Domain.DTOs.DashboardDTOs;

namespace steptreck.Web.ViewModel;

public sealed class ProjectTeamDashboardViewModel
{
    private readonly HttpClient _http;

    public ProjectTeamDashboardViewModel(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProjectDashboardDto?> GetProjectAsync(int projectId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProjectDashboardDto>($"api/dashboards/projects/{projectId}", ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<TeamDashboardDto?> GetTeamAsync(int projectId, int teamId, CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<TeamDashboardDto>($"api/dashboards/projects/{projectId}/teams/{teamId}", ct);
        }
        catch
        {
            return null;
        }
    }
}
