using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Services.Chat;

namespace steptreck.API.Controllers.Chat
{
    [ApiController]
    [Route("api/team-chats")]
    [Authorize]
    public class TeamChatsController : ControllerBase
    {
        private readonly ChatService _chat;

        public TeamChatsController(ChatService chat)
        {
            _chat = chat;
        }

        [HttpGet("{teamId:int}/messages")]
        public async Task<IActionResult> GetMessages(int teamId, [FromQuery] long? beforeId, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            var items = await _chat.GetLastMessages(teamId, beforeId, take, ct);
            return Ok(items);
        }
        [HttpGet("{teamId}/usernames")]
        public async Task<IActionResult> GetUsernames(
            int teamId,
            CancellationToken ct
        )
        {
            var result = await _chat.GetTeamUsernamesAsync(teamId, ct);

            return Ok(result);
        }
        [Authorize]
        [HttpGet("chats")]
        public async Task<IActionResult> GetChats(CancellationToken ct)
        {
            var chats = await _chat.GetChatsAsync(ct);
            return Ok(chats);
        }
    }
}
