using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Middleware;
using steptreck.API.Services.Subscriptions;
using steptreck.Domain.DTOs.SubscriptionsDTOs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace steptreck.API.Controllers.Subscriptions
{
    [SkipSubscriptionCheck]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly UserHelper _userHelper;
        private readonly ISubscriptionGate _gate;

        public SubscriptionsController(
            SubscriptionService subscriptionService,
            UserHelper userHelper,
            ISubscriptionGate gate)
        {
            _subscriptionService = subscriptionService;
            _userHelper = userHelper;
            _gate = gate;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] BySubDto model, CancellationToken ct)
        {
            if (model == null)
                return BadRequest("Пустое тело запроса.");

            if (model.PlanId <= 0)
                return BadRequest("PlanId должен быть > 0.");

            if (model.MonthCount <= 0)
                return BadRequest("MonthCount должен быть > 0.");

            try
            {
                await _subscriptionService.SubscribeToPlanAsync(model, ct);
                return Ok(new { message = "Подписка оформлена." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent(CancellationToken ct)
        {
            var dto = await _subscriptionService.GetCurrentAsync(ct);
            return Ok(dto);
        }

        [HttpGet("active")]
        public async Task<ActionResult<bool>> HasActive(CancellationToken ct)
        {
            var orgId = _userHelper.GetCurrentOrganizationId();
            var ok = await _gate.HasActiveAsync(orgId, ct);
            return Ok(ok);
        }
        [HttpGet("check")]
        public async Task<SubCheckInfoDto> Check(CancellationToken ct)
        {
            var sub = await _subscriptionService.CheckSubAsync(ct);
            return sub;
        }

        public sealed class ExtendSubscriptionDto
        {
            public int MonthCount { get; set; }
        }

        [HttpPost("extend")]
        public async Task<IActionResult> Extend([FromBody] ExtendSubscriptionDto model, CancellationToken ct)
        {
            if (model == null)
                return BadRequest("Пустое тело запроса.");

            if (model.MonthCount <= 0)
                return BadRequest("MonthCount должен быть > 0.");

            try
            {
                await _subscriptionService.ExtendActiveSubscriptionAsync(model.MonthCount, ct);
                return Ok(new { message = "Подписка продлена." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel(CancellationToken ct)
        {
            try
            {
                await _subscriptionService.CancelActiveSubscriptionAsync(ct);
                return Ok(new { message = "Подписка отменена." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [AllowAnonymous]
        [HttpGet("config")]
        [ProducesResponseType(typeof(List<SubscriptionItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<SubscriptionItemDto>>> GetConfig(CancellationToken ct)
        {
            try
            {
                var result = await _subscriptionService.GetConfigAsync(ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("custom")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<long>> CreateCustomSubscription(
            [FromBody] CreateCustomSubDto model,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var paymentId = await _subscriptionService.CreateCustomSubAsync(model, ct);
                return Ok(paymentId);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
