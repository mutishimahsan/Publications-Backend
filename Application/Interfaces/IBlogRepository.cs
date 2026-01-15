using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogRepository : IRepository<Blog>
    {
        Task<Blog?> GetBySlugAsync(string slug);
        Task<IEnumerable<Blog>> GetPublishedBlogsAsync();
        Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(Guid categoryId);
        Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm);
        Task IncrementViewCountAsync(Guid blogId);
    }
}
