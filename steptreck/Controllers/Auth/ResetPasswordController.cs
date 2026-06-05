using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Auth;
using steptreck.Domain.DTOs.AuthDTOs;

namespace steptreck.API.Controllers.Auth
{
    [Route("api/reset-password")]
    [ApiController]
    [SkipSubscriptionCheck]
    public class ResetPasswordController : ControllerBase
    {
        private readonly ResetPasswordService _service;

        public ResetPasswordController(ResetPasswordService service)
        {
            _service = service;
        }

        [HttpPost("resetlink")]
        public async Task<IActionResult> ResetLink([FromBody] EmailDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                await _service.SendResetLinkAsync(model.Email);
                return Ok("Если пользователь существует, ссылка была отправлена");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                await _service.ResetPasswordAsync(model);
                return Ok("Пароль успешно изменен. Эту страницу можно закрыть");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("check-reset-token")]
        public async Task<IActionResult> CheckResetToken([FromQuery] string token)
        {
            try
            {
                await _service.CheckTokenReset(token);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
