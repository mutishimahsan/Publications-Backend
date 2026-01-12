using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum InvoiceStatus
    {
        Draft = 0,
        Issued = 1,
        Sent = 2,
        Paid = 3,
        Overdue = 4,
        Cancelled = 5
    }
}
