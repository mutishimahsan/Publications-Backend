using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IDigitalAccessRepository : IRepository<DigitalAccess>
    {
        Task<DigitalAccess?> GetByOrderItemIdAsync(Guid orderItemId);
        Task<DigitalAccess?> GetByTokenAsync(string token);
        Task<IEnumerable<DigitalAccess>> GetByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<DigitalAccess>> GetByProductIdAsync(Guid productId);
        Task<IEnumerable<DigitalAccess>> GetExpiredAccessAsync();
        Task<IEnumerable<DigitalAccess>> GetActiveAccessByCustomerAsync(Guid customerId);
        Task<DigitalAccess?> GetValidAccessAsync(Guid orderItemId, Guid customerId);
        Task<int> GetCustomerDownloadCountAsync(Guid customerId, Guid productId);
        Task<bool> HasAccessToProductAsync(Guid customerId, Guid productId);
    }
}
