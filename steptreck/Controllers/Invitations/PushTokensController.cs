using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.Domain.DTOs;

[ApiController]
[Authorize]
[Route("api/push-tokens")]
public class PushTokensController : ControllerBase
{
    private readonly PushTokenService _pushTokenService;

    public PushTokensController(PushTokenService pushTokenService)
    {
        _pushTokenService = pushTokenService;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken ct
    )
    {
        await _pushTokenService.RegisterAsync(request, ct);

        return Ok(new
        {
            message = "Push token saved"
        });
    }
}