using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.API.Services.SessonWork;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.WorkSessionDTOs;

namespace steptreck.API.Controllers.WorkSession
{
    [Route("api/work-sessions")]
    [ApiController]
    public class WorkSessionController : ControllerBase
    {
        private readonly WorkSessionService _service;

        public WorkSessionController(WorkSessionService service)
        {
            _service = service;
        }

        [HttpPost("start")]
        public async Task<ActionResult<ApiMessageDto>> StartSession()
        {
            try
            {
                await _service.StartSessionAsync();
                return Ok(new ApiMessageDto { Message = "Сессия запущена." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }

        [HttpPost("toggle")]
        public async Task<ActionResult<ApiMessageDto>> ToggleSession()
        {
            try
            {
                await _service.ToogleSessionAsync();
                return Ok(new ApiMessageDto { Message = "Состояние сессии обновлено." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }

        [HttpPost("stop")]
        public async Task<ActionResult<ApiMessageDto>> StopSession()
        {
            try
            {
                await _service.StopSession();
                return Ok(new ApiMessageDto { Message = "Сессия остановлена." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<ActiveEmployeeSessionDto>>> GetActiveSessions()
        {
            var result = await _service.GetCurrentEmployeeSessionsAsync();
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<WorkSessionHistoryDto>>> GetCompletedSessions()
        {
            var result = await _service.GetCompletedSessionsAsync();
            return Ok(result);
        }

        [HttpGet("active/team/{teamId}")]
        public async Task<ActionResult<List<ActiveEmployeeSessionDto>>> GetActiveTeamSessions(int teamId)
        {
            var result = await _service.GetCurrentEmployeeSessionsTeamAsync(teamId);
            return Ok(result);
        }

        [HttpGet("history/team/{teamId}")]
        public async Task<ActionResult<List<WorkSessionHistoryDto>>> GetCompletedTeamSessions(int teamId)
        {
            var result = await _service.GetCompletedSessionsTeamAsync(teamId);
            return Ok(result);
        }

        [HttpGet("active/project/{projectId}")]
        public async Task<ActionResult<List<ActiveEmployeeSessionDto>>> GetActiveProjectSessions(int projectId)
        {
            var result = await _service.GetCurrentEmployeeSessionsProjectAsync(projectId);
            return Ok(result);
        }

        [HttpGet("history/project/{projectId}")]
        public async Task<ActionResult<List<WorkSessionHistoryDto>>> GetCompletedProjectSessions(int projectId)
        {
            var result = await _service.GetCompletedSessionsProjectAsync(projectId);
            return Ok(result);
        }



        [HttpGet("profile/{userId:int}/current")]
        public async Task<ActionResult<UserCurrentSessionDto>> GetUserCurrentOrLastSession(int userId)
        {
            var result = await _service.GetUserCurrentOrLastSessionAsync(userId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("history/{userId:int}")]
        public async Task<ActionResult<List<WorkSessionHistoryDto>>> GetUserSessionHistory(int userId)
        {
            var result = await _service.GetUserCompletedSessionsAsync(userId);
            return Ok(result);
        }

        [HttpGet("my-history")]
        public async Task<ActionResult<List<WorkSessionHistoryDto>>> GetMySessionHistory()
        {
            var result = await _service.GetMyCompletedSessionsAsync();
            return Ok(result);
        }
    }
}
