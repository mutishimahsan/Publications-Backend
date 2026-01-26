using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class DigitalAccessRepository : Repository<DigitalAccess>, IDigitalAccessRepository
    {
        public DigitalAccessRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<DigitalAccess?> GetByOrderItemIdAsync(Guid orderItemId)
        {
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(da => da.Customer)
                .FirstOrDefaultAsync(da => da.OrderItemId == orderItemId);
        }

        public async Task<DigitalAccess?> GetByTokenAsync(string token)
        {
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(da => da.Customer)
                .FirstOrDefaultAsync(da => da.CurrentToken == token && da.IsActive);
        }

        public async Task<IEnumerable<DigitalAccess>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(da => da.CustomerId == customerId)
                .OrderByDescending(da => da.AccessGrantedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DigitalAccess>> GetByProductIdAsync(Guid productId)
        {
            return await _dbSet
                .Include(da => da.OrderItem)
                .Include(da => da.Customer)
                .Where(da => da.ProductId == productId)
                .OrderByDescending(da => da.AccessGrantedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DigitalAccess>> GetExpiredAccessAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(da => da.Customer)
                .Where(da => da.IsActive &&
                    ((da.AccessExpiresAt.HasValue && da.AccessExpiresAt.Value < now) ||
                     (da.DownloadCount >= da.MaxDownloads)))
                .ToListAsync();
        }

        public async Task<IEnumerable<DigitalAccess>> GetActiveAccessByCustomerAsync(Guid customerId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(da => da.CustomerId == customerId &&
                    da.IsActive &&
                    (!da.AccessExpiresAt.HasValue || da.AccessExpiresAt.Value > now) &&
                    da.DownloadCount < da.MaxDownloads)
                .OrderByDescending(da => da.AccessGrantedAt)
                .ToListAsync();
        }

        public async Task<DigitalAccess?> GetValidAccessAsync(Guid orderItemId, Guid customerId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(da =>
                    da.OrderItemId == orderItemId &&
                    da.CustomerId == customerId &&
                    da.IsActive &&
                    (!da.AccessExpiresAt.HasValue || da.AccessExpiresAt.Value > now) &&
                    da.DownloadCount < da.MaxDownloads);
        }

        public async Task<int> GetCustomerDownloadCountAsync(Guid customerId, Guid productId)
        {
            return await _dbSet
                .Where(da => da.CustomerId == customerId && da.ProductId == productId)
                .SumAsync(da => da.DownloadCount);
        }

        public async Task<bool> HasAccessToProductAsync(Guid customerId, Guid productId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .AnyAsync(da =>
                    da.CustomerId == customerId &&
                    da.ProductId == productId &&
                    da.IsActive &&
                    (!da.AccessExpiresAt.HasValue || da.AccessExpiresAt.Value > now) &&
                    da.DownloadCount < da.MaxDownloads);
        }

        public override async Task<DigitalAccess?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(da => da.OrderItem)
                    .ThenInclude(oi => oi.Order)
                .Include(da => da.Product)
                .Include(da => da.Customer)
                .FirstOrDefaultAsync(da => da.Id == id);
        }
    }
}
