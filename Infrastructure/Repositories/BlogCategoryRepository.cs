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
    public class BlogCategoryRepository : Repository<BlogCategory>, IBlogCategoryRepository
    {
        public BlogCategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<BlogCategory?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted);
        }

        public async Task<IEnumerable<BlogCategory>> GetAllWithBlogCountAsync()
        {
            return await _dbSet
                .Include(c => c.Blogs.Where(b => !b.IsDeleted && b.IsPublished))
                .Where(c => !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _dbSet
                .AnyAsync(c => c.Name.ToLower() == name.ToLower() && !c.IsDeleted);
        }
    }
}
