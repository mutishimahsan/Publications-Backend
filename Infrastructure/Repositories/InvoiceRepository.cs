using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _dbSet
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<Invoice>> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Include(i => i.Order)
                .Where(i => i.OrderId == orderId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _dbSet
                .Include(i => i.Order)
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            return await _dbSet
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .Where(i => i.Status == status)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus status)
        {
            var invoice = await GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                invoice.Status = status;
                await UpdateAsync(invoice);
            }
        }

        public async Task IncrementDownloadCountAsync(Guid invoiceId)
        {
            var invoice = await GetByIdAsync(invoiceId);
            if (invoice != null)
            {
                invoice.DownloadCount++;
                invoice.LastDownloadedAt = DateTime.UtcNow;
                await UpdateAsync(invoice);
            }
        }

        public override async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.Id == id);
        }
    }
}
