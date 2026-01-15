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
    public class BlogRepository : Repository<Blog>, IBlogRepository
    {
        public BlogRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Blog?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(b => b.BlogCategories)
                .FirstOrDefaultAsync(b => b.Slug == slug && !b.IsDeleted);
        }

        public async Task<IEnumerable<Blog>> GetPublishedBlogsAsync()
        {
            return await _dbSet
                .Include(b => b.BlogCategories)
                .Where(b => !b.IsDeleted && b.IsPublished)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(b => b.BlogCategories)
                .Where(b => !b.IsDeleted &&
                           b.IsPublished &&
                           b.BlogCategories.Any(bc => bc.Id == categoryId))
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetPublishedBlogsAsync();

            var term = searchTerm.ToLower();
            return await _dbSet
                .Include(b => b.BlogCategories)
                .Where(b => !b.IsDeleted &&
                           b.IsPublished &&
                           (b.Title.ToLower().Contains(term) ||
                            b.Content.ToLower().Contains(term) ||
                            b.Excerpt.ToLower().Contains(term)))
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task IncrementViewCountAsync(Guid blogId)
        {
            var blog = await GetByIdAsync(blogId);
            if (blog != null)
            {
                blog.ViewCount++;
                await UpdateAsync(blog);
            }
        }

        public override async Task<IEnumerable<Blog>> GetAllAsync()
        {
            return await _dbSet
                .Include(b => b.BlogCategories)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public override async Task<Blog?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.BlogCategories)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }
    }
}
