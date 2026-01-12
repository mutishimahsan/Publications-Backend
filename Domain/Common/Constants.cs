using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public static class RoleConstants
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string FinanceAdmin = "FinanceAdmin";
        public const string ContentAdmin = "ContentAdmin";
        public const string SupportAdmin = "SupportAdmin";
        public const string Customer = "Customer";

        public static string[] GetAllRoles()
        {
            return new[] { SuperAdmin, FinanceAdmin, ContentAdmin, SupportAdmin, Customer };
        }
    }

    public static class PolicyConstants
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireFinance = "RequireFinance";
        public const string RequireContent = "RequireContent";
        public const string RequireSupport = "RequireSupport";
        public const string RequireCustomer = "RequireCustomer";
    }

    public static class OrderConstants
    {
        public const string OrderNumberPrefix = "MENT-";
        public const string InvoiceNumberPrefix = "INV-";

        public static string GenerateOrderNumber()
        {
            return $"{OrderNumberPrefix}{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        public static string GenerateInvoiceNumber()
        {
            return $"{InvoiceNumberPrefix}{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }
    }
}
