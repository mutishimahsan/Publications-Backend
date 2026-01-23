using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(string status);
        Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto);
        Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid invoiceId, string status);
        Task<string> GenerateInvoicePdfAsync(Guid invoiceId);
        Task<byte[]> GetInvoicePdfBytesAsync(Guid invoiceId);
        Task<string> GetInvoiceDownloadUrlAsync(Guid invoiceId);
        Task<bool> SendInvoiceEmailAsync(Guid invoiceId);
        Task<bool> RecordInvoiceDownloadAsync(Guid invoiceId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
