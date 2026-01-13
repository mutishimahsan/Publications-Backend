using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendOrderConfirmationAsync(Guid orderId);
        Task SendPaymentConfirmationAsync(Guid paymentId);
        Task SendInvoiceAsync(Guid invoiceId);
        Task SendDownloadLinkAsync(Guid orderItemId);
        Task SendPasswordResetAsync(string email, string resetToken);
    }
}
