using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Middleware;
using steptreck.API.Services.ImportExport;

namespace steptreck.API.Controllers.Database
{
    [SkipSubscriptionCheck]
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class ImportExportController : ControllerBase
    {
        private readonly ImportExportDataService _dataService;

        public ImportExportController(ImportExportDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet("export/{tableName}")]
        public async Task<IActionResult> ExportTable([FromRoute] string tableName)
        {
            try
            {
                var csv = await _dataService.ExportTableToCsvAsync(tableName);
                var fileName = $"{tableName}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка экспорта: {ex.Message}");
            }
        }


        [HttpPost("import/{tableName}")]
        public async Task<IActionResult> ImportTable([FromRoute] string tableName, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не найден или пуст.");

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync();

                await _dataService.ImportCsvToTableAsync(tableName, content);
                return Ok($"✅ Импорт завершен успешно: {tableName}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка импорта: {ex.Message}");
            }
        }

        [HttpPost("sql")]
        public async Task<IActionResult> ExecuteSql([FromBody] string sql)
        {
            try
            {
                var result = await _dataService.ExecuteSqlAsync(sql);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка выполнения SQL: {ex.Message}");
            }
        }
    }
}
