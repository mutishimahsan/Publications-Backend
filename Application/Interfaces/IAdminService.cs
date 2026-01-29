using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        // Dashboard
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<UserActivityReportDto> GetUserActivityReportAsync(DateTime startDate, DateTime endDate);
        Task<SystemHealthDto> GetSystemHealthAsync();

        // Audit Logs
        Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter);
        Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id);
        Task<bool> ClearOldAuditLogsAsync(int daysToKeep);

        // System Management
        Task<string> BackupDatabaseAsync();
        Task<bool> ClearCacheAsync();
        Task<bool> SendTestEmailAsync(string email);
        Task<bool> GenerateSampleDataAsync();
    }
}
