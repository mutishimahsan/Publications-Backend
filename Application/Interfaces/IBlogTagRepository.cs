using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogTagRepository : IRepository<BlogTag>
    {
        Task<BlogTag?> GetBySlugAsync(string slug);
        Task<IEnumerable<BlogTag>> GetByBlogIdAsync(Guid blogId);
        Task<bool> ExistsByNameAsync(string name);
    }
}
