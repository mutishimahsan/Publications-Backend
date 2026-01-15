using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Author : AuditableEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Affiliation { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? PhotoUrl { get; set; }

        public virtual ICollection<ProductAuthor> ProductAuthors { get; set; } = new List<ProductAuthor>();
    }
}

public class ProductAuthor : BaseEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public virtual Author Author { get; set; } = null!;

    public AuthorRole Role { get; set; } = AuthorRole.Author;
    public int DisplayOrder { get; set; } = 0;
}