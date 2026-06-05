using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using steptreck.API.Middleware;
using steptreck.API.Services.Auth;
using steptreck.Domain.DTOs.AuthDTOs;

namespace steptreck.API.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    [SkipSubscriptionCheck]
    public class RegisterController : ControllerBase
    {
        private readonly RegisterService _registerService;
        private readonly TurnstileService _turnstileService;

        public RegisterController(RegisterService registerService, TurnstileService turnstileService)
        {
            _registerService = registerService;
            _turnstileService = turnstileService;
        }

        /// <summary>
        /// Регистрация организации и владельца
        /// </summary>
        [HttpPost("organization")]
        public async Task<IActionResult> RegisterOrganization(
            [FromBody] RegisterOrgDTO model,
            CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var captchaOk = await _turnstileService.VerifyAsync(
                model.CaptchaToken,
                ip
            );

            if (!captchaOk)
                return BadRequest("Captcha failed");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _registerService.RegisterOrgAsync(model, ct);
                return Ok(new { message = "Организация успешно зарегистрирована" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var captchaOk = await _turnstileService.VerifyAsync(
                model.CaptchaToken,
                ip
            );

            try
            {
                await _registerService.RegisterUserAsync(model, ct);

                return Ok(new
                {
                    message = "Пользователь успешно зарегистрирован"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
