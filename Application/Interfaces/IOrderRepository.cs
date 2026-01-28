using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOrderRepository : IRepository<Domain.Entities.Order>
    {
        Task<Domain.Entities.Order?> GetByOrderNumberAsync(string orderNumber);
        Task<OrderItem?> GetOrderItemByIdAsync(Guid orderItemId);
        Task<IEnumerable<Domain.Entities.Order>> GetByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Domain.Entities.Order>> GetOrdersByStatusAsync(Domain.Enums.OrderStatus status);
        Task<IEnumerable<Domain.Entities.Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task UpdateOrderStatusAsync(Guid orderId, Domain.Enums.OrderStatus status);
        Task UpdatePaymentStatusAsync(Guid orderId, Domain.Enums.PaymentStatus status);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
