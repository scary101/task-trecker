using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Projects;
using steptreck.Domain.DTOs.TeamDTOs;

namespace steptreck.API.Controllers.Projects
{
    [SkipSubscriptionCheck]
    [ApiController] 
    [Route("api/team-files")]
    [Authorize]
    public class TeamFilesController : ControllerBase
    {
        private readonly TeamFileServise _service;

        public TeamFilesController(TeamFileServise service)
        {
            _service = service;
        }
        [HttpPost("teams/{teamId:int}")]
        [RequestSizeLimit(50L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50L * 1024 * 1024)]
        public async Task<ActionResult<TeamFileReadDto>> Upload(
            [FromRoute] int teamId,
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            var dto = await _service.AddTeamFileAsync(teamId, file, ct);
            return Ok(dto);
        }

        [HttpGet("{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            var dto = await _service.DownloadTeamFileAsync(attachmentId, ct);
            return File(dto.Content, dto.ContentType, dto.FileName);
        }

        [HttpDelete("{attachmentId:int}")]
        public async Task<IActionResult> Delete(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            await _service.DeleteTeamFileAsync(attachmentId, ct);
            return NoContent();
        }

        [HttpGet("teams/{teamId:int}")]
        public async Task<IActionResult> GetTeamFiles(
            [FromRoute] int teamId,
            CancellationToken ct)
        {
            var files = await _service.GetTeamFilesAsync(teamId, ct);
            return Ok(files);
        }
    }
}
