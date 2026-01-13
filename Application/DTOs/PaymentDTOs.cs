using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";

        // Gateway details
        public string? GatewayTransactionId { get; set; }

        // Offline details
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? DepositSlipUrl { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class ProcessOnlinePaymentDto
    {
        public Guid OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? CardToken { get; set; } // For Stripe
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvc { get; set; }
    }

    public class ProcessOfflinePaymentDto
    {
        public Guid OrderId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public IFormFile DepositSlip { get; set; } = null!;
    }

    public class ApproveOfflinePaymentDto
    {
        public Guid PaymentId { get; set; }
        public bool Approve { get; set; }
        public string? Notes { get; set; }
    }
}
