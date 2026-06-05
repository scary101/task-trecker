using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Owner;
using steptreck.Domain.DTOs.OwnerDashboardDTOs;

namespace steptreck.API.Controllers.Owner;

[ApiController]
[Authorize(Policy = "OwnerOnly")]
[SkipSubscriptionCheck]
[Route("api/owner/dashboard")]
public class OwnerDashboardController : ControllerBase
{
    private readonly OwnerDashboardService _dashboard;

    public OwnerDashboardController(OwnerDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OwnerDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OwnerDashboardDto>> Get(CancellationToken ct)
    {
        var dto = await _dashboard.GetDashboardAsync(ct);
        return Ok(dto);
    }
}
