using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Format { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? UnitPrice;
        public decimal TotalPrice => FinalPrice * Quantity;
        public bool IsDigital => Format == "Digital" || Format == "Bundle";
    }

    public class CartDto
    {
        public Guid? CartId { get; set; }
        public Guid? UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();

        public decimal Subtotal => Items.Sum(i => i.TotalPrice);
        public decimal TaxAmount => Subtotal * 0.15m; // 15% tax example
        public decimal TotalAmount => Subtotal + TaxAmount;
        public int TotalItems => Items.Sum(i => i.Quantity);

        public bool ContainsDigitalProducts => Items.Any(i => i.IsDigital);
        public bool ContainsPhysicalProducts => Items.Any(i => !i.IsDigital);

        public DateTime CreatedAt { get; internal set; }
        public DateTime UpdatedAt { get; internal set; }
    }

    public class AddToCartDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDto
    {
        public int Quantity { get; set; }
    }
}
