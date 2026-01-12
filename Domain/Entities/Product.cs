using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Product : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ISBN { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? TableOfContents { get; set; }
        public string? IntendedAudience { get; set; }

        // Pricing
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }

        // Inventory
        public int StockQuantity { get; set; }
        public int? MaxDownloads { get; set; } // For digital products

        // Format
        public ProductFormat Format { get; set; }
        public ProductType Type { get; set; }

        // Media
        public string? CoverImageUrl { get; set; }
        public string? SampleFileUrl { get; set; }
        public string? DigitalFileUrl { get; set; }

        // Status
        public ProductStatus Status { get; set; }
        public DateTime? PublishedDate { get; set; }

        // Relationships
        public Guid? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public virtual ICollection<ProductAuthor> ProductAuthors { get; set; } = new List<ProductAuthor>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
