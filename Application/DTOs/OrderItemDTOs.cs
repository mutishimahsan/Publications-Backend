using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Format { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal TotalPrice => (DiscountPrice ?? UnitPrice) * Quantity;

        // For digital products
        public int DownloadsUsed { get; set; }
        public int? MaxDownloads { get; set; }
        public DateTime? LastDownloadedAt { get; set; }
        public bool IsDownloadable { get; set; }
        public string? DigitalFileUrl { get; set; }
        public string? SecureDownloadUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemDetailDto : OrderItemDto
    {
        public string? ProductDescription { get; set; }
        public string? ProductISBN { get; set; }
        public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
    }
}
