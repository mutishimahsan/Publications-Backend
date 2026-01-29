using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalBlogs { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int PendingComments { get; set; }
        public int LowStockProducts { get; set; }
    }

    public class RevenueReportDto
    {
        public DateTime Period { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class SalesReportDto
    {
        public List<DailySalesDto> DailySales { get; set; } = new();
        public List<ProductSalesDto> TopProducts { get; set; } = new();
        public List<CategorySalesDto> CategorySales { get; set; } = new();
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class ProductSalesDto
    {
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySalesDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class UserActivityReportDto
    {
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
        public List<UserLoginDto> RecentLogins { get; set; } = new();
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public string IPAddress { get; set; } = string.Empty;
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string OldValues { get; set; } = string.Empty;
        public string NewValues { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IPAddress { get; set; } = string.Empty;
    }

    public class AuditLogFilterDto
    {
        public string? UserId { get; set; }
        public string? EntityName { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SystemHealthDto
    {
        public string DatabaseStatus { get; set; } = string.Empty;
        public string EmailServiceStatus { get; set; } = string.Empty;
        public string StorageStatus { get; set; } = string.Empty;
        public DateTime LastBackup { get; set; }
        public decimal DiskUsagePercentage { get; set; }
        public decimal MemoryUsagePercentage { get; set; }
        public bool IsHealthy { get; set; }
    }
}
