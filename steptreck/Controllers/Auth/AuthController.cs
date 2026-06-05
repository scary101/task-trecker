using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Ocsp;
using steptreck.API.Middleware;
using steptreck.API.Models;
using steptreck.API.Services.Auth;
using steptreck.Domain.DTOs.AuthDTOs;

namespace steptreck.API.Controllers.Auth
{
    [SkipSubscriptionCheck]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly TurnstileService _turnstileService;
        private readonly NotificationsService _notificationsService;

        public AuthController(AuthService authService, TurnstileService turnstileService, NotificationsService notificationsService)
        {
            _authService = authService;
            _turnstileService = turnstileService;
            _notificationsService = notificationsService;
        }

        [AllowAnonymous]
        [HttpPost("login-code")]
        public async Task<IActionResult> SendLoginCode([FromBody] LoginDTO model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var captchaOk = await _turnstileService.VerifyAsync(
                model.CaptchaToken,
                ip
            );

            if (!captchaOk)
                return BadRequest("Captcha failed");

            try
            {
                await _authService.SendLoginCodeAsync(model, ct);

                return Ok(new
                {
                    message = "Код отправлен на почту",
                    expiresInMinutes = 10
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("не найден", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { error = ex.Message });

                if (ex.Message.Contains("Неверный", StringComparison.OrdinalIgnoreCase))
                    return Unauthorized(new { error = ex.Message });

                return BadRequest(new { error = ex.Message });
            }
        }
        [AllowAnonymous]
        [HttpPost("mobile/login-code")]
        public async Task<IActionResult> SendLoginCodeMobile([FromBody] LoginDTO model, CancellationToken ct)
        {

            try
            {
                await _authService.SendLoginCodeAsync(model, ct);

                return Ok(new
                {
                    message = "Код отправлен на почту",
                    expiresInMinutes = 10
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("не найден", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { error = ex.Message });

                if (ex.Message.Contains("Неверный", StringComparison.OrdinalIgnoreCase))
                    return Unauthorized(new { error = ex.Message });

                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var token = await _authService.VerifyCodeAsync(model, ct);

                return Ok(token);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("is-member")]
        public async Task<ActionResult<IsMemberResponse>> IsMebmer(CancellationToken ct)
        {
            var isMember = await _authService.IsMemberAsync(ct);

            return Ok(new IsMemberResponse
            {
                IsMember = isMember
            });
        }
        [AllowAnonymous]
        [HttpPost("login-notifivation")]
        public async Task<IActionResult> LoginWithNotification([FromBody] LoginDTO model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var captchaOk = await _turnstileService.VerifyAsync(
                model.CaptchaToken,
                ip
            );

            if (!captchaOk)
                return BadRequest("Captcha failed");

            try
            {
                var chellenge = await _authService.CreateLoginChallengeAsync(model, ct);

                await _notificationsService.SendLoginChallengePush(chellenge, ct);

                return Ok(new LoginChallengeCreatedDto
                {
                    ChallengeId = chellenge.Id,
                    ExpiresInSeconds = 120
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("не найден", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { error = ex.Message });

                if (ex.Message.Contains("Неверный", StringComparison.OrdinalIgnoreCase))
                    return Unauthorized(new { error = ex.Message });

                return BadRequest(new { error = ex.Message });
            }

        }

        [AllowAnonymous]
        [HttpPost("login-notification")]
        public Task<IActionResult> LoginWithNotificationAlias([FromBody] LoginDTO model, CancellationToken ct)
        {
            return LoginWithNotification(model, ct);
        }

        [Authorize]
        [HttpPost("login-challenges/{challengeId:guid}/confirm")]
        public async Task<IActionResult> ConfirmLoginChallenge(
            Guid challengeId,
            CancellationToken ct)
        {
            try
            {
                await _authService.ConfirmLoginChallengeAsync(challengeId, ct);
                return Ok();
            }
            catch (InvalidOperationException ex) when (ex.Message == "Challenge not found.")
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("login-challenges/{challengeId:guid}/token")]
        public async Task<IActionResult> GetLoginChallengeToken(
            Guid challengeId,
            CancellationToken ct)
        {
            try
            {
                var token = await _authService.TryCompleteApprovedLoginChallengeAsync(challengeId, ct);

                if (token is null)
                    return Accepted(new { status = "pending" });

                return Ok(token);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Challenge not found.")
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



    }
}
