using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public string TableName { get; set; } = string.Empty;
        public Guid RecordId { get; set; }
        public string Action { get; set; } = string.Empty; // Create, Update, Delete
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
