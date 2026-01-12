using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Payment : AuditableEntity
    {
        public string PaymentReference { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public Guid? CustomerId { get; set; }
        public virtual User? Customer { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentType Type { get; set; }
        public PaymentStatus Status { get; set; }

        // Amounts
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";

        // Dates
        public DateTime? ProcessedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        // Gateway details
        public string? GatewayTransactionId { get; set; }
        public string? GatewayResponse { get; set; }

        // Offline payment details
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? DepositSlipUrl { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
