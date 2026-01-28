using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogCategoryRepository : IRepository<BlogCategory>
    {
        Task<BlogCategory?> GetBySlugAsync(string slug);
        Task<IEnumerable<BlogCategory>> GetAllWithBlogCountAsync();
        Task<bool> ExistsByNameAsync(string name);
    }
}
