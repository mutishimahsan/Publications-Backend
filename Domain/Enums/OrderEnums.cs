using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3,
        Refunded = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 1,
        Paid = 2,
        PartiallyRefunded = 3,
        FullyRefunded = 4,
        Failed = 5
    }

    public enum FulfillmentStatus
    {
        Unfulfilled = 0,
        PartiallyFulfilled = 1,
        Fulfilled = 2,
        Delivered = 3
    }
}
