using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Services.Subscriptions;

namespace steptreck.API.Controllers.Subscriptions
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly PlanServise _service;

        public PlansController(PlanServise service)
        {
            _service = service;
        }

        /// <summary>
        /// Публично: список всех тарифных планов (для главной страницы)
        /// GET: api/plans
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var plans = await _service.GetAllAsync(ct);
            return Ok(plans);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOne([FromRoute] int id, CancellationToken ct)
        {
            var plan = await _service.GetByIdAsync(id, ct);
            if (plan is null)
                return NotFound(new { error = "План не найден" });

            return Ok(plan);
        }
    }
}
