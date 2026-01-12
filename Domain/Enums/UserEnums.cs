using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum UserType
    {
        PublicVisitor = 0,
        GuestCustomer = 1,
        RegisteredCustomer = 2,
        Admin = 3,
        FinanceAdmin = 4,
        ContentAdmin = 5,
        SupportAdmin = 6,
        SuperAdmin = 7
    }
}
