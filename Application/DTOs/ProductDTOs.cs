using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ISBN { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? TableOfContents { get; set; }
        public string? IntendedAudience { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price;

        public int StockQuantity { get; set; }
        public int? MaxDownloads { get; set; }

        public string Format { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public string? CoverImageUrl { get; set; }
        public string? SampleFileUrl { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime? PublishedDate { get; set; }

        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ISBN { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? TableOfContents { get; set; }
        public string? IntendedAudience { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }

        public int StockQuantity { get; set; }
        public int? MaxDownloads { get; set; }

        public ProductFormat Format { get; set; }
        public ProductType Type { get; set; }

        public Guid? CategoryId { get; set; }

        public List<Guid> AuthorIds { get; set; } = new List<Guid>();

        // For digital products
        public IFormFile? DigitalFile { get; set; }
        public IFormFile? CoverImage { get; set; }
        public IFormFile? SampleFile { get; set; }
    }

    public class UpdateProductDto
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? ISBN { get; set; }
        public string? Description { get; set; }
        public string? TableOfContents { get; set; }
        public string? IntendedAudience { get; set; }

        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }

        public int? StockQuantity { get; set; }
        public int? MaxDownloads { get; set; }

        public ProductFormat? Format { get; set; }
        public ProductType? Type { get; set; }
        public ProductStatus? Status { get; set; }

        public Guid? CategoryId { get; set; }

        public List<Guid>? AuthorIds { get; set; }

        public IFormFile? DigitalFile { get; set; }
        public IFormFile? CoverImage { get; set; }
        public IFormFile? SampleFile { get; set; }
    }
}
