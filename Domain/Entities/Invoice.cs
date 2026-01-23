using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Invoice : AuditableEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public Guid? CustomerId { get; set; }
        public virtual User? Customer { get; set; }

        // Invoice details
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public InvoiceStatus Status { get; set; }

        // Amounts
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // File storage
        public string? FilePath { get; set; }
        public string? FileUrl { get; set; }
        public DateTime? EmailedAt { get; set; }

        // Audit
        public int DownloadCount { get; set; } = 0;
        public DateTime? LastDownloadedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
