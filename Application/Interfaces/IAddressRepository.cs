using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IEnumerable<Address>> GetByUserIdAsync(Guid userId);
        Task<Address?> GetDefaultShippingAddressAsync(Guid userId);
        Task<Address?> GetDefaultBillingAddressAsync(Guid userId);
        Task<bool> SetDefaultShippingAddressAsync(Guid addressId, Guid userId);
        Task<bool> SetDefaultBillingAddressAsync(Guid addressId, Guid userId);
    }
}
