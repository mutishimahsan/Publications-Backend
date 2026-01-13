using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        public AddressDto? ShippingAddress { get; set; }
        public AddressDto? BillingAddress { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string FulfillmentStatus { get; set; } = string.Empty;

        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public List<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
        public List<InvoiceDto> Invoices { get; set; } = new List<InvoiceDto>();

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    public class CreateOrderDto
    {
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerName { get; set; }

        public CreateAddressDto? ShippingAddress { get; set; }
        public CreateAddressDto? BillingAddress { get; set; }
        public bool UseShippingAsBilling { get; set; } = true;

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentType PaymentType { get; set; }

        // For offline payments
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? TransactionId { get; set; }
        public IFormFile? DepositSlip { get; set; }

        public List<OrderItemRequestDto> Items { get; set; } = new List<OrderItemRequestDto>();
    }

    public class OrderItemRequestDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
