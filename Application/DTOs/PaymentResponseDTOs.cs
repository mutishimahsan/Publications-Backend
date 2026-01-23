using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PaymentSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentDto? Payment { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class BankAccountDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
