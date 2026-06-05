using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.Backup;

namespace steptreck.API.Controllers.Database
{
    [SkipSubscriptionCheck]
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class BackupController : ControllerBase
    {
        private readonly BackupService _backupService;

        public BackupController(BackupService backupService)
        {
            _backupService = backupService;
        }
        [HttpGet("download")]
        public async Task<IActionResult> Download()
        {
            var bytes = await _backupService.CreateBackupAsync();

            if (bytes == null || bytes.Length == 0)
            {
                return BadRequest("Backup failed, no data generated!");
            }

            return File(bytes, "application/octet-stream", "backup.sql");
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBackup([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран.");

            await _backupService.RestoreBackupAsync(file);

            return Ok(new { message = "База восстановлена" });
        }
    }
}
