using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.Domain.DTOs;

namespace steptreck.API.Controllers.Notifications
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationsService _service;

        public NotificationsController(NotificationsService service)
        {
            _service = service;
        }

        [HttpGet]
        public Task<List<NotificationDto>> GetLatest([FromQuery] int take = 30, CancellationToken ct = default)
            => _service.GetMyLatest(take, ct);

        [HttpGet("unread-count")]
        public Task<int> GetUnreadCount(CancellationToken ct = default)
            => _service.GetMyUnreadCount(ct);

        [HttpPost("team")]
        public async Task<IActionResult> SendToTeam([FromBody] CreateTeamNotificationDto model, CancellationToken ct)
        {
            var count = await _service.CreateForTeam(model.TeamId, model.Text, ct);
            return Ok(new
            {
                message = count > 0
                    ? "Уведомление отправлено"
                    : "В команде нет получателей для уведомления",
                count
            });
        }

        [HttpPost("member")]
        public async Task<IActionResult> SendToMember([FromBody] CreateMemberNotificationDto model, CancellationToken ct)
        {
            await _service.CreateForTeamMember(model.TeamId, model.MemberId, model.Text, ct);
            return Ok(new { message = "Уведомление отправлено" });
        }

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
        {
            await _service.MarkRead(id, ct);
            return Ok();
        }
    }
}
