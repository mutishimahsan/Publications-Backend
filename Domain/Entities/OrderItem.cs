using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal TotalPrice => (DiscountPrice ?? UnitPrice) * Quantity;

        // For digital products
        public int DownloadsUsed { get; set; } = 0;
        public DateTime? LastDownloadedAt { get; set; }
        public bool IsDownloadable => Product.Format == ProductFormat.Digital ||
                                      Product.Format == ProductFormat.Bundle;
    }
}
