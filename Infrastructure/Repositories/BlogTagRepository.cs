using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class BlogTagRepository : Repository<BlogTag>, IBlogTagRepository
    {
        public BlogTagRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<BlogTag?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted);
        }

        public async Task<IEnumerable<BlogTag>> GetByBlogIdAsync(Guid blogId)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted && t.Blogs.Any(b => b.Id == blogId))
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _dbSet
                .AnyAsync(t => t.Name.ToLower() == name.ToLower() && !t.IsDeleted);
        }
    }
}
