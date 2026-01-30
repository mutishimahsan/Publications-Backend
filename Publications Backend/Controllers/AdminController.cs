using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        [HttpGet("sales-report")]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var report = await _adminService.GetSalesReportAsync(startDate, endDate);
            return Ok(report);
        }

        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivityReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var report = await _adminService.GetUserActivityReportAsync(startDate, endDate);
            return Ok(report);
        }

        [HttpGet("system-health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            var health = await _adminService.GetSystemHealthAsync();
            return Ok(health);
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
        {
            var logs = await _adminService.GetAuditLogsAsync(filter);
            return Ok(logs);
        }

        [HttpGet("audit-logs/{id}")]
        public async Task<IActionResult> GetAuditLogById(Guid id)
        {
            var log = await _adminService.GetAuditLogByIdAsync(id);
            if (log == null)
                return NotFound();

            return Ok(log);
        }

        [HttpDelete("audit-logs/clear-old")]
        public async Task<IActionResult> ClearOldAuditLogs([FromQuery] int daysToKeep = 30)
        {
            var result = await _adminService.ClearOldAuditLogsAsync(daysToKeep);
            return result ? Ok() : BadRequest("Failed to clear old audit logs");
        }

        [HttpPost("backup")]
        public async Task<IActionResult> BackupDatabase()
        {
            try
            {
                var backupPath = await _adminService.BackupDatabaseAsync();
                return Ok(new { message = "Backup created successfully", path = backupPath });
            }
            catch (Exception ex)
            {
                return BadRequest($"Backup failed: {ex.Message}");
            }
        }

        [HttpPost("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            var result = await _adminService.ClearCacheAsync();
            return result ? Ok("Cache cleared successfully") : BadRequest("Failed to clear cache");
        }

        [HttpPost("send-test-email")]
        public async Task<IActionResult> SendTestEmail([FromBody] string email)
        {
            var result = await _adminService.SendTestEmailAsync(email);
            return result ? Ok("Test email sent successfully") : BadRequest("Failed to send test email");
        }

        [HttpPost("generate-sample-data")]
        public async Task<IActionResult> GenerateSampleData()
        {
            var result = await _adminService.GenerateSampleDataAsync();
            return result ? Ok("Sample data generated successfully") : BadRequest("Failed to generate sample data");
        }
    }
}
