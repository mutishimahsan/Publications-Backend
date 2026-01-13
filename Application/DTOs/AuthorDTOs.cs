using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AuthorDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Affiliation { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? PhotoUrl { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAuthorDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Affiliation { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public IFormFile? Photo { get; set; }
    }

    public class UpdateAuthorDto
    {
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public string? Affiliation { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public IFormFile? Photo { get; set; }
    }

    public class ProductAuthorDto
    {
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
