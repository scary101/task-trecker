using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Projects;
using steptreck.API.Services.Tasks;
using steptreck.API.Services.WorkUser;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.TeamDTOs;
using steptreck.Domain.DTOs.TeamDTOs.MemberDTOs;

namespace steptreck.API.Controllers.Projects
{
    [SkipSubscriptionCheck]
    [ApiController]
    [Route("api/project-teams")]
    public class ProjectTeamsController : ControllerBase
    {
        private readonly ProjectTeamService _service;
        private readonly AvatarService _avatarService;

        public ProjectTeamsController(ProjectTeamService service, AvatarService avatarService)
        {
            _service = service;
            _avatarService = avatarService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam(CreateTeamDto model, CancellationToken ct)
        {
            try
            {
                var team = await _service.CreateTeam(model, ct);
                return Ok(new TeamReadDto
                {
                    Id = team.Id,
                    ProjectId = team.ProjectId,
                    Name = team.Name,
                    Description = team.Description,
                    CardBackgroundUrl = team.CardBackgroundUrl,
                    IsActive = team.IsActive,
                    CreatedAt = team.CreatedAt,
                    UpdatedAt = team.UpdatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiMessageDto { Error = ex.Message });
            }
        }

        [HttpPost("lead")]
        public async Task<IActionResult> AddLead(CreateLeadMebmerProjectDto model, CancellationToken ct)
        {
            await _service.AddLeadToTeam(model, ct);
            return Ok(new { message = "Руководитель назначен" });
        }

        [HttpPost("members")]
        public async Task<IActionResult> AddMember(CreateMebmerProjectDto model, CancellationToken ct)
        {
            await _service.AddMemberToTeam(model, ct);
            return Ok(new { message = "Участник добавлен" });
        }

        [HttpPut("members/role")]
        public async Task<IActionResult> UpdateMemberRole(UpdateTeamRoleDto model, CancellationToken ct)
        {
            await _service.UpdateTeamRole(model, ct);
            return Ok(new { message = "Роль обновлена" });
        }

        [HttpDelete("members/{memberId:int}")]
        public async Task<IActionResult> DeleteMember(int memberId, CancellationToken ct)
        {
            await _service.DeleteMebmerProject(memberId, ct);
            return Ok(new { message = "Участник удалён" });
        }

        [HttpDelete("{teamId:int}/members/{memberId:int}")]
        public async Task<IActionResult> DeleteTeamMember(int teamId, int memberId, CancellationToken ct)
        {
            try
            {
                await _service.DeleteMemberFromTeam(teamId, memberId, ct);
                return Ok(new { message = "Участник удалён" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }
        [HttpGet("leads-free/{projectId:int}")]
        public async Task<IActionResult> GetLeadsWithoutTeam(int projectId, CancellationToken ct)
        {
            var result = await _service.GetProjectLeadsWithoutTeam(projectId, ct);
            return Ok(result);
        }
        [HttpGet("free/{projectId:int}")]
        public async Task<IActionResult> GetMembersWithoutTeam(int projectId, CancellationToken ct)
        {
            var result = await _service.GetMembersWithoutTeam(projectId, ct);
            return Ok(result);
        }

        [HttpGet("project/{projectId:int}")]
        public async Task<IActionResult> GetTeams(int projectId, CancellationToken ct)
        {
            var teams = await _service.GetByProject(projectId, ct);
            return Ok(teams);
        }

        [HttpPut("{teamId:int}")]
        public async Task<IActionResult> UpdateTeam(int teamId, UpdateTeamDto model, CancellationToken ct)
        {
            try
            {
                await _service.UpdateTeam(teamId, model, ct);
                return Ok(new ApiMessageDto { Message = "Команда обновлена" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiMessageDto { Error = ex.Message });
            }
        }

        [HttpGet("{teamId:int}")]
        public async Task<IActionResult> GetTeam(int teamId, CancellationToken ct)
        {
            var team = await _service.GetTeam(teamId, ct);
            return Ok(team);
        }

        [HttpGet("{teamId:int}/members")]
        public async Task<IActionResult> GetTeamMembers(int teamId, CancellationToken ct)
        {
            var members = await _service.GetTeamMembers(teamId, ct);
            return Ok(members);
        }

        [HttpDelete("{teamId:int}")]
        public async Task<IActionResult> DeleteTeam(int teamId, CancellationToken ct)
        {
            try
            {
                await _service.DeleteTeam(teamId, ct);
                return Ok(new { message = "Команда удалена" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }
        [HttpPost("{teamId:int}/background")]
        public async Task<IActionResult> UploadTeamBackground(
        int teamId,
        IFormFile file,
        CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var url = await _avatarService.UploadTeamBackgroundAsync(
                teamId,
                file,
                baseUrl,
                ct);

            return Ok(new { url });
        }
    }
}
