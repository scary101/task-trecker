using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Middleware;
using steptreck.API.Services.Members;
using steptreck.Domain.DTOs;
using steptreck.Domain.DTOs.AuthDTOs;
using steptreck.Domain.DTOs.InviteDTO;

namespace steptreck.API.Controllers.Invitations
{
    [SkipSubscriptionCheck]
    [Route("api/[controller]")]
    [ApiController]
    public class InviteController : ControllerBase
    {
        private readonly InviteServise _inviteServise;
        private readonly UserHelper _userHelper;

        public InviteController(InviteServise inviteServise, UserHelper userHelper)
        {
            _inviteServise = inviteServise;
            _userHelper = userHelper;
        }



        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember([FromBody] RegisterMebmerDto model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            try
            {
                await _inviteServise.SendInvite(model, ct);

                return Ok(new { message = "Приглашение отправлено" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiMessageDto
                {
                    Error = "VALIDATION_ERROR",
                    Message = "Некорректные данные"
                });
            }

            try
            {
                var currentUserId = _userHelper.GetCurrentUserId();

                if (currentUserId == null)
                {
                    return Unauthorized(new ApiMessageDto
                    {
                        Error = "AUTH_REQUIRED",
                        Message = "Для принятия приглашения необходимо войти в аккаунт"
                    });
                }

                try
                {
                    var result = await _inviteServise.AcceptInviteAsync(
                        model.Token,
                        currentUserId,
                        ct);

                    if (!result.Success)
                    {
                        return BadRequest(new ApiMessageDto
                        {
                            Error = "INVITE_ACCEPT_FAILED",
                            Message = result.Message ?? "Не удалось принять приглашение"
                        });
                    }

                    return Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiMessageDto
                    {
                        Error = "FORBIDDEN",
                        Message = "У вас нет доступа к этому приглашению"
                    });
                }
                catch (InvalidOperationException)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiMessageDto
                    {
                        Error = "INVITE_PROCESSING_ERROR",
                        Message = "Произошла ошибка при обработке приглашения"
                    });
                }
            }
            catch
            {
                return Unauthorized(new ApiMessageDto
                {
                    Error = "AUTH_REQUIRED",
                    Message = "Для принятия приглашения необходимо войти в аккаунт"
                });
            }

            
        }
    }
}
