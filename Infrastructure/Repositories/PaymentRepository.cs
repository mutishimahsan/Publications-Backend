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
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            return await _dbSet
                .Include(p => p.Order)
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PaymentReference == reference);
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Include(p => p.Order)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPendingOfflinePaymentsAsync()
        {
            return await _dbSet
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Where(p => p.Type == PaymentType.Offline &&
                           p.Status == PaymentStatus.Pending)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status)
        {
            var payment = await GetByIdAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                payment.ProcessedAt = status == PaymentStatus.Paid ? DateTime.UtcNow : null;
                await UpdateAsync(payment);
            }
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            return await query
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.Amount);
        }
    }
}
