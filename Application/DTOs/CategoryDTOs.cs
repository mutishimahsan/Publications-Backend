using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public Guid? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }

        public List<CategoryDto> SubCategories { get; set; } = new List<CategoryDto>();
        public int ProductCount { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
        public Guid? ParentCategoryId { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public int? DisplayOrder { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }
}
