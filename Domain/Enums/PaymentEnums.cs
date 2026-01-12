using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum PaymentMethod
    {
        Stripe = 0,
        PayFast = 1,
        BankTransfer = 2,
        CashDeposit = 3,
        Manual = 4
    }

    public enum PaymentType
    {
        Online = 0,
        Offline = 1
    }
}
