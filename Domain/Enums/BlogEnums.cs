using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum BlogStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2
    }

    public enum CommentStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Spam = 3
    }
}
