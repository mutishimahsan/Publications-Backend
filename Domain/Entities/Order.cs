using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Order : AuditableEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public virtual User Customer { get; set; } = null!;

        // Order details
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerName { get; set; }

        // Shipping
        public Guid? ShippingAddressId { get; set; }
        public virtual Address? ShippingAddress { get; set; }

        public Guid? BillingAddressId { get; set; }
        public virtual Address? BillingAddress { get; set; }

        // Financials
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Status
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public FulfillmentStatus FulfillmentStatus { get; set; }

        // Dates
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Relationships
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
