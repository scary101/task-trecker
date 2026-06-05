using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services;

namespace steptreck.API.Controllers.Subscriptions
{
    [SkipSubscriptionCheck]
    [ApiController]
    [Route("api/receipts")]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly ReceiptPdfService _receiptPdf;

        public ReceiptsController(ReceiptPdfService receiptPdf)
        {
            _receiptPdf = receiptPdf;
        }
        [HttpGet("{paymentId:long}/receipt")]
        public async Task<IActionResult> DownloadReceipt(long paymentId, CancellationToken ct)
        {

            try
            {
                var (stream, contentType, fileName) =
                    await _receiptPdf.DownloadReceiptAsync(paymentId, ct);

                return File(stream, contentType, fileName);
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
