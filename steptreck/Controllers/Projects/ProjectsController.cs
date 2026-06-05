using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Projects;
using steptreck.API.Services.WorkUser;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.ProjectDTOs;

namespace steptreck.API.Controllers.Projects
{
    [SkipSubscriptionCheck]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectServise _projectService;
        private readonly AvatarService _avatarService;

        public ProjectsController(ProjectServise projectService, AvatarService avatarService)
        {
            _projectService = projectService;
            _avatarService = avatarService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto, CancellationToken ct)
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
                var created = await _projectService.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProjectUpdateDto dto, CancellationToken ct)
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
                var updated = await _projectService.UpdateAsync(id, dto, ct);
                return Ok(updated);
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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false, CancellationToken ct = default)
        {
            var list = await _projectService.GetAllAsync(includeArchived, ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] bool includeArchived = false, CancellationToken ct = default)
        {
            try
            {
                var project = await _projectService.GetByIdAsync(id, includeArchived, ct);
                return Ok(project);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/toggle-archive")]
        public async Task<IActionResult> ToggleArchive([FromRoute] int id, CancellationToken ct = default)
        {
            try
            {
                var result = await _projectService.ToggleArchiveAsync(id, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            try
            {
                await _projectService.DeleteAsync(id, ct);
                return Ok(new { message = "Проект удалён" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiMessageDto { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiMessageDto { Error = ex.Message });
            }
        }
        [HttpPost("{projectId:int}/background")]
        public async Task<IActionResult> UploadProjectBackground(
            int projectId,
            IFormFile file,
            CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var url = await _avatarService.UploadProjectBackgroundAsync(
                projectId,
                file,
                baseUrl,
                ct);

            return Ok(new { url });
        }
    }
}
