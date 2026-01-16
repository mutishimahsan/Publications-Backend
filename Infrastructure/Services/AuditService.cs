using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public interface IAuditService
    {
        Task LogAuditAsync(string tableName, Guid recordId, string action, string? oldValues = null, string? newValues = null);
    }

    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAuditAsync(string tableName, Guid recordId, string action, string? oldValues = null, string? newValues = null)
        {
            var userId = GetCurrentUserId();
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                UserId = userId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString()
            };

            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Return system user ID if not authenticated
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }
}
