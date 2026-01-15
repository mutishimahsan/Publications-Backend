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
    public class AddressRepository : Repository<Address>, IAddressRepository
    {
        public AddressRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefaultShipping)
                .ThenByDescending(a => a.IsDefaultBilling)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Address?> GetDefaultShippingAddressAsync(Guid userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefaultShipping);
        }

        public async Task<Address?> GetDefaultBillingAddressAsync(Guid userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefaultBilling);
        }

        public async Task<bool> SetDefaultShippingAddressAsync(Guid addressId, Guid userId)
        {
            // Reset all shipping defaults for this user
            var addresses = await _dbSet
                .Where(a => a.UserId == userId && a.IsDefaultShipping)
                .ToListAsync();

            foreach (var address in addresses)
            {
                address.IsDefaultShipping = false;
                await UpdateAsync(address);
            }

            // Set new default
            var targetAddress = await GetByIdAsync(addressId);
            if (targetAddress != null && targetAddress.UserId == userId)
            {
                targetAddress.IsDefaultShipping = true;
                await UpdateAsync(targetAddress);
                return true;
            }

            return false;
        }

        public async Task<bool> SetDefaultBillingAddressAsync(Guid addressId, Guid userId)
        {
            // Reset all billing defaults for this user
            var addresses = await _dbSet
                .Where(a => a.UserId == userId && a.IsDefaultBilling)
                .ToListAsync();

            foreach (var address in addresses)
            {
                address.IsDefaultBilling = false;
                await UpdateAsync(address);
            }

            // Set new default
            var targetAddress = await GetByIdAsync(addressId);
            if (targetAddress != null && targetAddress.UserId == userId)
            {
                targetAddress.IsDefaultBilling = true;
                await UpdateAsync(targetAddress);
                return true;
            }

            return false;
        }
    }
}
