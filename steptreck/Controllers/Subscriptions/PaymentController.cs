using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Services.Subscriptions;

namespace steptreck.API.Controllers.Subscriptions
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _payments;
        private readonly ReceiptPdfService _receipts;

        public PaymentsController(PaymentService payments, ReceiptPdfService receipts)
        {
            _payments = payments;
            _receipts = receipts;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? provider = null,
            [FromQuery] int? subscriptionId = null,
            CancellationToken ct = default)
        {
            var items = await _payments.GetListAsync(page, pageSize, status, provider, subscriptionId, ct);
            return Ok(items);
        }

        [HttpGet("{paymentId:long}")]
        public async Task<IActionResult> Get(long paymentId, CancellationToken ct)
        {
            try
            {
                var dto = await _payments.GetByIdAsync(paymentId, ct);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }


        [HttpPost("{paymentId:long}/receipt")]
        public async Task<IActionResult> GenerateReceipt(long paymentId, CancellationToken ct)
        {
            if (paymentId <= 0)
                return BadRequest(new { error = "paymentId должен быть > 0." });

            try
            {
                var objectKey = await _receipts.GenerateAndStoreAsync(paymentId, ct);
                return Ok(new { objectKey });
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
    }
}