using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IEnumerable<Invoice>> GetByOrderIdAsync(Guid orderId);
        Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status);
        Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus status);
        Task IncrementDownloadCountAsync(Guid invoiceId);
    }
}
