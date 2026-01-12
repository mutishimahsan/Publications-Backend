using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Blog : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? FeaturedImageUrl { get; set; }

        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedDate { get; set; }
        public int ViewCount { get; set; } = 0;

        public virtual ICollection<BlogCategory> BlogCategories { get; set; } = new List<BlogCategory>();
    }
}

public class BlogCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
}