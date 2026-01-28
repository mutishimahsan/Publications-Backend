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
        Task<Blog?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Blog>> GetPublishedBlogsAsync();
        Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(Guid categoryId);
        Task<IEnumerable<Blog>> GetBlogsByTagAsync(string tag);
        Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count);
        Task<IEnumerable<Blog>> GetPopularBlogsAsync(int count);
        Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm);
        Task<int> GetTotalBlogCountAsync();
        Task<int> GetPublishedBlogCountAsync();
        Task IncrementViewCountAsync(Guid blogId);
        Task<bool> SlugExistsAsync(string slug);
    }
}
