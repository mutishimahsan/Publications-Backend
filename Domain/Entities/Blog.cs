using Domain.Entities;
using Domain.Enums;
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

        // Navigation properties
        public virtual ICollection<BlogCategory> BlogCategories { get; set; } = new List<BlogCategory>();
        public virtual ICollection<BlogTag> BlogTags { get; set; } = new List<BlogTag>();
        public virtual ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
        public Guid? AuthorId { get; set; }
        public virtual User? Author { get; set; }
        public BlogStatus Status { get; set; } = BlogStatus.Draft;
    }

    public class BlogCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }

    public class BlogTag : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }

    public class BlogComment : AuditableEntity
    {
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public CommentStatus Status { get; set; } = CommentStatus.Pending;
        public Guid BlogId { get; set; }
        public Guid? ParentCommentId { get; set; }

        // Navigation properties
        public virtual Blog Blog { get; set; } = null!;
        public virtual BlogComment? ParentComment { get; set; }
        public virtual ICollection<BlogComment> Replies { get; set; } = new List<BlogComment>();
        public bool IsApproved { get; set; }
    }
}