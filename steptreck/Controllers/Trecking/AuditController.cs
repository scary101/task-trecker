using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;

namespace steptreck.API.Controllers.Trecking
{
    [SkipSubscriptionCheck]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly AuditService _auditService;

        public AuditController(AuditService auditService)
        {
            _auditService = auditService;
        }
        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> GetAudit([FromQuery] string? tableName)
        {
            try
            {
                var logs = await _auditService.GetLogsAsync(tableName);
                return Ok(logs);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("error")]
        public async Task<IActionResult> Error()
        {
            return StatusCode(500);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("org")]
        public async Task<IActionResult> GetAllLogs(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? action = null,
    [FromQuery] string? actor = null,
    [FromQuery] string? dateFrom = null,
    [FromQuery] string? dateTo = null,
    [FromQuery] string? sortBy = "date",
    [FromQuery] bool sortDesc = true,
    CancellationToken ct = default)
        {
            var result = await _auditService.GetAllLogsAsync(
                page, pageSize, search, action, actor, dateFrom, dateTo, sortBy, sortDesc, ct);

            return Ok(result);
        }

        [HttpGet("projects/{projectId:int}")]
        public async Task<IActionResult> GetProjectLogs(
            int projectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? action = null,
            [FromQuery] string? actor = null,
            [FromQuery] string? dateFrom = null,
            [FromQuery] string? dateTo = null,
            CancellationToken ct = default)
        {
            var result = await _auditService.GetProjectLogsAsync(
                projectId, page, pageSize, action, actor, dateFrom, dateTo, ct);

            return Ok(result);
        }

        [HttpGet("teams/{teamId:int}")]
        public async Task<IActionResult> GetTeamLogs(
            int teamId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? action = null,
            [FromQuery] string? actor = null,
            [FromQuery] string? dateFrom = null,
            [FromQuery] string? dateTo = null,
            CancellationToken ct = default)
        {
            var result = await _auditService.GetTeamLogsAsync(
                teamId, page, pageSize, action, actor, dateFrom, dateTo, ct);

            return Ok(result);
        }

        [HttpGet("members/{memberId:int}")]
        public async Task<IActionResult> GetMemberLogs(
            int memberId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? action = null,
            [FromQuery] string? actor = null,
            [FromQuery] string? dateFrom = null,
            [FromQuery] string? dateTo = null,
            CancellationToken ct = default)
        {
            var result = await _auditService.GetMemberLogsAsync(
                memberId, page, pageSize, action, actor, dateFrom, dateTo, ct);

            return Ok(result);
        }



    }
}
