using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Tasks;
using steptreck.Domain.DTOs.TaskDTOs;
using steptreck.Domain.DTOs.TaskDTOs.CheckListDTOs;

namespace steptreck.API.Controllers.Tasks
{
    [SkipSubscriptionCheck]
    [ApiController]
    [Route("api/tasks")]
    public class ProjectTaskChecklistController : ControllerBase
    {
        private readonly CheckListServise _checklist;

        public ProjectTaskChecklistController(CheckListServise checklist)
        {
            _checklist = checklist;
        }

        [HttpPost("{taskId:int}/checklist/complete-next")]
        public async Task<IActionResult> CompleteNext(int taskId, CancellationToken ct)
        {
            if (taskId <= 0)
                return BadRequest(new { message = "Некорректный taskId" });

            try
            {
                await _checklist.CompleteNextChecklistItemAsync(taskId, ct);
                return Ok(new { message = "Следующий пункт чек-листа отмечен как выполненный" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("checklist/items")]
        public async Task<IActionResult> PutItem([FromBody] PutCheckListItem model, CancellationToken ct)
        {
            if (model is null)
                return BadRequest(new { message = "Тело запроса пустое" });

            if (model.Id <= 0)
                return BadRequest(new { message = "Некорректный Id" });

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Название пункта не может быть пустым" });

            try
            {
                model.Name = name;

                await _checklist.PutItem(model, ct);
                return Ok(new { message = "Пункт чек-листа обновлён" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpPut("{taskId:int}/checklist")]
        public async Task<IActionResult> SaveChecklist(int taskId, [FromBody] SaveChecklistDto dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest(new { message = "Пустое тело" });
            if (dto.TaskId != taskId) return BadRequest(new { message = "TaskId не совпадает" });

            await _checklist.SaveChecklistAsync(dto, ct);
            return Ok(new { message = "Чек-лист сохранён" });
        }

    }

}
