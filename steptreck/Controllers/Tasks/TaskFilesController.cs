using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Tasks;
using steptreck.Domain.DTOs.TeamDTOs;

namespace steptreck.API.Controllers.Tasks
{
    [SkipSubscriptionCheck]
    [Route("api/task-files")]
    [ApiController]
    public class TaskFilesController : ControllerBase
    {
        private readonly TaskFileService _service;

        public TaskFilesController(TaskFileService service)
        {
            _service = service;
        }

        [HttpPost("tasks/{taskId:int}")]
        [RequestSizeLimit(50L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50L * 1024 * 1024)]
        public async Task<ActionResult<TeamFileReadDto>> Upload(
            [FromRoute] int taskId,
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            var dto = await _service.AddTaskFileAsync(taskId, file, ct);
            return Ok(dto);
        }

        [HttpGet("{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            var dto = await _service.DownloadTaskFileAsync(attachmentId, ct);
            return File(dto.Content, dto.ContentType, dto.FileName);
        }

        [HttpDelete("{attachmentId:int}")]
        public async Task<IActionResult> Delete(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            await _service.DeleteTaskFileAsync(attachmentId, ct);
            return NoContent();
        }

        [HttpGet("tasks/{taskId:int}")]
        public async Task<IActionResult> GetTeamFiles(
            [FromRoute] int taskId,
            CancellationToken ct)
        {
            var files = await _service.GetTaskFilesAsync(taskId, ct);
            return Ok(files);
        }
    }
}
