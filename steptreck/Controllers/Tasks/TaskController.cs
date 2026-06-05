using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Tasks;
using steptreck.Domain.DTOs.TaskDTOs;

namespace steptreck.API.Controllers.Tasks
{
    [SkipSubscriptionCheck]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskForTeamDto model, CancellationToken ct)
        {
            if (model == null) return BadRequest("Модель не передана");

            var id = await _taskService.CreateTaskForLead(model, ct);

            return CreatedAtAction(nameof(GetById), new { taskId = id }, new { id });
        }


        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetById([FromRoute] int taskId, CancellationToken ct)
        {
            var task = await _taskService.GetTaskByIdAsync(taskId, ct);
            if (task == null) return NotFound();

            return Ok(task);
        }

        [HttpPost("{taskId:int}/complete")]
        public async Task<IActionResult> Complete([FromRoute] int taskId, CancellationToken ct)
        {
            await _taskService.CompleteTaskAsync(taskId, ct);
            return NoContent();
        }

        [HttpDelete("{taskId:int}")]
        public async Task<IActionResult> Delete([FromRoute] int taskId, CancellationToken ct)
        {
            await _taskService.DeleteTaskAsync(taskId, ct);
            return Ok(new { message = "Задача удалена" });
        }


        [HttpGet("team/{teamId:int}")]
        public async Task<IActionResult> GetTeamTasks(
            [FromRoute] int teamId,
            [FromQuery] TaskListFilterDto? filter,
            CancellationToken ct
        )
        {
            var tasks = await _taskService.GetTeamTasksAsync(teamId, filter, ct);
            return Ok(tasks);
        }


        [HttpGet("member/{memberId:int}")]
        public async Task<IActionResult> GetMemberTasks(
            [FromRoute] int memberId,
            [FromQuery] TaskListFilterDto? filter,
            CancellationToken ct
        )
        {
            var tasks = await _taskService.GetMemberTasksAsync(memberId, filter, ct);
            return Ok(tasks);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyTasks(
            [FromQuery] TaskListFilterDto? filter,
            CancellationToken ct
        )
        {
            var tasks = await _taskService.GetMyTasksAsync(filter, ct);
            return Ok(tasks);
        }
        [HttpPut("deadline")]
        public async Task<IActionResult> PutDeadline([FromBody] PutDeadLineDto model, CancellationToken ct)
        {
            if (model.TaskId <= 0)
                return BadRequest("Некорректный TaskId");

            await _taskService.PutDeadLine(model, ct);

            return Ok(new
            {
                message = "Дедлайн обновлён",
                deadline = model.Date
            });
        }
    }
}
