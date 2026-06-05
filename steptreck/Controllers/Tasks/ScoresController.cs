using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Tasks;
using steptreck.Domain.DTOs.MemberDTOs;

namespace steptreck.API.Controllers.Tasks
{
    [SkipSubscriptionCheck]
    [Route("api/[controller]")]
    [ApiController]
    public class ScoresController : ControllerBase
    {
        private readonly MemberScoreService _scores;

        public ScoresController(MemberScoreService scores)
        {
            _scores = scores;
        }
        [HttpGet("me")]
        [ProducesResponseType(typeof(ScoreDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ScoreDto>> GetMyScore(CancellationToken ct)
        {
            var dto = await _scores.GetMyScore(ct);
            return Ok(dto);
        }
        [HttpGet("member/{memberId:int}")]
        [ProducesResponseType(typeof(ScoreDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ScoreDto>> GetScoremebmer(int memberId, CancellationToken ct)
        {
            var dto = await _scores.GetScoreMember(memberId, ct);
            return Ok(dto);
        }

        [HttpGet("teams/{teamId:int}")]
        [ProducesResponseType(typeof(List<ScoreRowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ScoreRowDto>>> GetTeamScores([FromRoute] int teamId, CancellationToken ct)
        {
            var dto = await _scores.GetTeamScores(teamId, ct);
            return Ok(dto);
        }
    }
}
