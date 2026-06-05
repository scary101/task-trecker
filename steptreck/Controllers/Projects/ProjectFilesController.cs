using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Projects;
using steptreck.Domain.DTOs.ProjectDTOs;

namespace steptreck.API.Controllers.Projects
{
    [SkipSubscriptionCheck]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectFilesController : ControllerBase
    {
        private readonly ProjectFileServise _projectFileServise;

        public ProjectFilesController(ProjectFileServise projectFileServise)
        {
            _projectFileServise = projectFileServise;
        }
        [HttpPost("{projectId:int}/files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            [FromRoute] int projectId,
            [FromForm] ProjectFileUploadDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Некорректные данные",
                    errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            k => k.Key,
                            v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                });

            try
            {
                var result = await _projectFileServise.AddProjectFileAsync(projectId, dto.File, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("files/{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            try
            {
                var file = await _projectFileServise.DownloadProjectFileAsync(attachmentId, ct);

                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{projectId:int}/files")]
        public async Task<IActionResult> GetFiles(
            [FromRoute] int projectId,
            CancellationToken ct)
        {
            var files = await _projectFileServise.GetProjectFilesAsync(projectId, ct);
            return Ok(files);
        }

        [HttpDelete("files/{attachmentId:int}")]
        public async Task<IActionResult> Delete(
            [FromRoute] int attachmentId,
            CancellationToken ct)
        {
            await _projectFileServise.DeleteProjectFileAsync(attachmentId, ct);
            return NoContent();
        }
    }
}
