using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Members;
using steptreck.API.Services.WorkUser;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.MemberDTOs;

namespace steptreck.API.Controllers.Members
{
    [ApiController]
    [SkipSubscriptionCheck]
    [Route("api/members")]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly MemberService _memberService;
        private readonly AvatarService _avatarService;

        public MembersController(MemberService memberService, AvatarService avatarService)
        {
            _memberService = memberService;
            _avatarService = avatarService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var members = await _memberService.GetAllAsync(ct);
            return Ok(members);
        }
        [HttpGet("project/{id:int}")]
        public async Task<IActionResult> GetByProject(int id, CancellationToken ct)
        {
            var members = await _memberService.GetByProject(id, ct);
            return Ok(members);
        }
        [SkipSubscriptionCheck]
        [HttpGet("me/fio")]
        public async Task<ActionResult<FioInfoDto>> GetMyFio(CancellationToken ct)
        {
            var fio = await _memberService.GetFioAsync(ct);

            if (fio is null)
                return NotFound(new ApiMessageDto
                {
                    Error = "Пользователь не является участником организации"
                });

            return Ok(fio);
        }
        [SkipSubscriptionCheck]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var dto = await _memberService.GetMyProfileAsync(ct);
            return Ok(dto);
        }
        [HttpGet("profile/{id:int}")]
        public async Task<IActionResult> GetProfile(int id, CancellationToken ct)
        {
            var dto = await _memberService.GetProfile(id, ct);
            return Ok(dto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            try
            {
                var member = await _memberService.GetByIdAsync(id, ct);
                return Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberDto model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                await _memberService.UpdateAsync(id, model, ct);
                return Ok(new { message = "Сотрудник обновлён" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _memberService.DeleteAsync(id, ct);
                return Ok(new { message = "Сотрудник деактивирован" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
        [SkipSubscriptionCheck]
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var url = await _avatarService.UploadMyAvatarAsync(file, baseUrl, ct);
            return Ok(new { avatarUrl = url });
        }
        [SkipSubscriptionCheck]
        [HttpPost("username")]
        public async Task<IActionResult> AddUsername([FromBody] UpdateUsernameDto? model, [FromQuery] string? username, CancellationToken ct)
        {
            if (model is not null && !ModelState.IsValid)
                return ValidationProblem(ModelState);

            var value = model?.Username ?? username;
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest(new ApiMessageDto { Error = "Никнейм обязателен" });

            try
            {
                var newusername = await _memberService.AddUserNameAsync(value, ct);
                return Ok(new { username = newusername });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }
        [SkipSubscriptionCheck]
        [HttpPut("username")]
        public async Task<IActionResult> ChanggeUsername([FromBody] UpdateUsernameDto? model, [FromQuery] string? username, CancellationToken ct)
        {
            if (model is not null && !ModelState.IsValid)
                return ValidationProblem(ModelState);

            var value = model?.Username ?? username;
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest(new ApiMessageDto { Error = "Никнейм обязателен" });

            try
            {
                var newusername = await _memberService.AddUserNameAsync(value, ct);
                return Ok(new { username = newusername });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiMessageDto { Error = ex.Message });
            }
        }
    }
}
