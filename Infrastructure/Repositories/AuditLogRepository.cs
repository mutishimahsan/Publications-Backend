using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId)
        {
            return await _dbSet
                .Where(al => al.TableName == entityName && al.RecordId == entityId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<int> DeleteOldLogsAsync(int daysToKeep)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _dbSet
                .Where(al => al.Timestamp < cutoffDate)
                .ToListAsync();

            _dbSet.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            return oldLogs.Count;
        }
    }
}
