using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum ProductFormat
    {
        Print = 0,
        Digital = 1,
        Bundle = 2
    }

    public enum ProductType
    {
        Book = 0,
        ExamGuide = 1,
        Report = 2,
        WhitePaper = 3,
        PDF = 4
    }

    public enum ProductStatus
    {
        Draft = 0,
        Published = 1,
        OutOfStock = 2,
        Discontinued = 3
    }
}
