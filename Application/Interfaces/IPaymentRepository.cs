using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPaymentRepository : IRepository<Domain.Entities.Payment>
    {
        Task<Domain.Entities.Payment?> GetByReferenceAsync(string reference);
        Task<IEnumerable<Domain.Entities.Payment>> GetByOrderIdAsync(Guid orderId);
        Task<IEnumerable<Domain.Entities.Payment>> GetPendingOfflinePaymentsAsync();
        Task UpdatePaymentStatusAsync(Guid paymentId, Domain.Enums.PaymentStatus status);
        Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
