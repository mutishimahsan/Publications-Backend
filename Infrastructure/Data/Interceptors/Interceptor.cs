using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
using System.Security.Claims;
using Domain.Entities;

namespace Infrastructure.Data.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is AppDbContext context)
            {
                await AuditChangesAsync(context);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task AuditChangesAsync(AppDbContext context)
        {
            var auditEntries = new List<AuditLog>();
            var userId = GetCurrentUserId();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged)
                    continue;

                // 🔥 Skip Identity internal tables
                if (entry.Metadata.ClrType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true)
                    continue;

                var primaryKey = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey());

                var recordId = primaryKey?.CurrentValue is Guid guid
                    ? guid
                    : Guid.Empty;

                var auditEntry = new AuditLog
                {
                    TableName = entry.Entity.GetType().Name,
                    RecordId = recordId,
                    Action = entry.State.ToString(),
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues =
                            JsonConvert.SerializeObject(entry.CurrentValues.ToObject(), Formatting.Indented);
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues =
                            JsonConvert.SerializeObject(entry.OriginalValues.ToObject(), Formatting.Indented);
                        break;

                    case EntityState.Modified:
                        auditEntry.OldValues =
                            JsonConvert.SerializeObject(entry.OriginalValues.ToObject(), Formatting.Indented);
                        auditEntry.NewValues =
                            JsonConvert.SerializeObject(entry.CurrentValues.ToObject(), Formatting.Indented);
                        break;
                }

                auditEntries.Add(auditEntry);
            }

            if (auditEntries.Any())
                await context.AuditLogs.AddRangeAsync(auditEntries);
        }



        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userIdClaim, out var userId)
                ? userId
                : Guid.Empty; // SYSTEM / background process
        }
    }
}