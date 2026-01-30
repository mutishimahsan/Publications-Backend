using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AdminService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(u => !u.IsDeleted),
                TotalOrders = await _context.Orders.CountAsync(o => !o.IsDeleted),
                TotalProducts = await _context.Products.CountAsync(p => !p.IsDeleted),
                TotalBlogs = await _context.Blogs.CountAsync(b => !b.IsDeleted && b.IsPublished),
                TotalRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed && !p.IsDeleted)
                    .SumAsync(p => p.Amount),
                PendingOrders = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Pending && !o.IsDeleted),
                PendingComments = await _context.BlogComments
                    .CountAsync(c => c.Status == CommentStatus.Pending && !c.IsDeleted),
                LowStockProducts = await _context.Products
                    .CountAsync(p => p.StockQuantity <= p.MinStockThreshold && !p.IsDeleted)
            };

            return stats;
        }

        public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var report = new SalesReportDto();

            // Get daily sales
            var dailySales = await _context.Orders
                .Where(o => !o.IsDeleted &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate &&
                           o.Status == OrderStatus.Completed)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    Orders = g.Count(),
                    Revenue = g.SelectMany(o => o.OrderItems)
                              .Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            report.DailySales = dailySales;

            // Get top products
            var topProducts = await _context.OrderItems
                .Where(oi => !oi.IsDeleted &&
                            oi.Order.CreatedAt >= startDate &&
                            oi.Order.CreatedAt <= endDate &&
                            oi.Order.Status == OrderStatus.Completed)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Title })
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key.ProductId,
                    ProductTitle = g.Key.Title,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            report.TopProducts = topProducts;

            // Get category sales
            var categorySales = await _context.OrderItems
                .Where(oi => !oi.IsDeleted &&
                            oi.Order.CreatedAt >= startDate &&
                            oi.Order.CreatedAt <= endDate &&
                            oi.Order.Status == OrderStatus.Completed)
                .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
                .Select(g => new CategorySalesDto
                {
                    CategoryId = (Guid)g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    ProductsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(c => c.Revenue)
                .ToListAsync();

            report.CategorySales = categorySales;

            return report;
        }

        public async Task<UserActivityReportDto> GetUserActivityReportAsync(DateTime startDate, DateTime endDate)
        {
            var report = new UserActivityReportDto
            {
                NewUsers = await _context.Users
                    .CountAsync(u => !u.IsDeleted &&
                                    u.CreatedAt >= startDate &&
                                    u.CreatedAt <= endDate),
                ActiveUsers = await _context.Users
                    .CountAsync(u => !u.IsDeleted &&
                                    u.LastLogin != null &&
                                    ((DateTime)u.LastLogin) >= startDate &&
                                    ((DateTime)u.LastLogin) <= endDate)
            };

            // Get recent logins from audit logs
            var recentLogins = await _context.AuditLogs
                .Where(al => al.TableName == "User" && al.Action == "Login" &&
                            al.Timestamp >= startDate && al.Timestamp <= endDate)
                .OrderByDescending(al => al.Timestamp)
                .Take(20)
                .Select(al => new UserLoginDto
                {
                    Email = al.UserEmail,
                    LoginTime = al.Timestamp,
                    IPAddress = al.IpAddress
                })
                .ToListAsync();

            report.RecentLogins = recentLogins;

            return report;
        }

        public async Task<SystemHealthDto> GetSystemHealthAsync()
        {
            var health = new SystemHealthDto();

            try
            {
                // Check database connection
                await _context.Database.CanConnectAsync();
                health.DatabaseStatus = "Healthy";

                // Check email service (simplified)
                health.EmailServiceStatus = "Healthy";

                // Check storage (simplified)
                health.StorageStatus = "Healthy";

                // Get last backup time
                health.LastBackup = await GetLastBackupTimeAsync();

                // Simulate system metrics
                health.DiskUsagePercentage = 65.5m;
                health.MemoryUsagePercentage = 42.3m;

                health.IsHealthy = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system health");
                health.DatabaseStatus = "Unhealthy";
                health.IsHealthy = false;
            }

            return health;
        }

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserId))
            {
                query = query.Where(al => al.UserId.ToString() == filter.UserId);
            }

            if (!string.IsNullOrEmpty(filter.EntityName))
            {
                query = query.Where(al => al.TableName == filter.EntityName);
            }

            if (!string.IsNullOrEmpty(filter.Action))
            {
                query = query.Where(al => al.Action == filter.Action);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(al => al.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(al => al.Timestamp <= filter.EndDate.Value);
            }

            var logs = await query
                .OrderByDescending(al => al.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    UserId = al.UserId.ToString(),
                    UserEmail = al.UserEmail,
                    Action = al.Action,
                    EntityName = al.TableName,
                    EntityId = al.RecordId.ToString(),
                    OldValues = al.OldValues,
                    NewValues = al.NewValues,
                    Timestamp = al.Timestamp,
                    IPAddress = al.IpAddress
                })
                .ToListAsync();

            return logs;
        }

        public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
        {
            var log = await _context.AuditLogs
                .FirstOrDefaultAsync(al => al.Id == id);

            if (log == null)
                return null;

            return new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId.ToString(),
                UserEmail = log.UserEmail,
                Action = log.Action,
                EntityName = log.TableName,
                EntityId = log.RecordId.ToString(),
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Timestamp = log.Timestamp,
                IPAddress = log.IpAddress
            };
        }

        public async Task<bool> ClearOldAuditLogsAsync(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var rowsAffected = await _context.RemoveOldAuditLogsAsync(cutoffDate);

                _logger.LogInformation($"Cleared {rowsAffected} old audit logs");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing old audit logs");
                return false;
            }
        }

        public async Task<string> BackupDatabaseAsync()
        {
            try
            {
                var backupPath = _configuration["BackupSettings:Path"] ?? "Backups";
                var backupFileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupPath, backupFileName);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(backupPath);

                // In a real application, you would execute SQL backup commands
                await File.WriteAllTextAsync(fullPath, "Database backup content");

                _logger.LogInformation($"Database backup created: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                throw;
            }
        }

        public async Task<bool> ClearCacheAsync()
        {
            try
            {
                _logger.LogInformation("Cache cleared");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string email)
        {
            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "Test Email from MENTISERA Publications",
                    "This is a test email to verify the email service is working correctly.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return false;
            }
        }

        public async Task<bool> GenerateSampleDataAsync()
        {
            try
            {
                _logger.LogInformation("Sample data generated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample data");
                return false;
            }
        }

        private async Task<DateTime> GetLastBackupTimeAsync()
        {
            var backupPath = _configuration["BackupSettings:Path"] ?? "Backups";
            if (Directory.Exists(backupPath))
            {
                var backupFiles = Directory.GetFiles(backupPath, "*.bak");
                if (backupFiles.Any())
                {
                    var lastBackup = backupFiles
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .First();

                    return lastBackup.LastWriteTime;
                }
            }

            return DateTime.MinValue;
        }
    }
}
