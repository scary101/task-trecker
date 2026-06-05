using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Dashboards;
using steptreck.Domain.DTOs.DashboardDTOs;

namespace steptreck.API.Controllers.Dashboards;

[ApiController]
[Authorize]
[SkipSubscriptionCheck]
[Route("api/dashboards")]
public sealed class ProjectTeamDashboardController : ControllerBase
{
    private readonly ProjectTeamDashboardService _dashboard;

    public ProjectTeamDashboardController(ProjectTeamDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("projects/{projectId:int}")]
    [ProducesResponseType(typeof(ProjectDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDashboardDto>> GetProject(int projectId, CancellationToken ct)
    {
        return Ok(await _dashboard.GetProjectDashboardAsync(projectId, ct));
    }

    [HttpGet("projects/{projectId:int}/teams/{teamId:int}")]
    [ProducesResponseType(typeof(TeamDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDashboardDto>> GetTeam(int projectId, int teamId, CancellationToken ct)
    {
        return Ok(await _dashboard.GetTeamDashboardAsync(projectId, teamId, ct));
    }
}
