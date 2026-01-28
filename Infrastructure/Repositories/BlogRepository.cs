using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Include(b => b.Comments.Where(c => c.IsApproved))
                .FirstOrDefaultAsync(b => b.Slug == slug && !b.IsDeleted);
        }

        public async Task<Blog?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Include(b => b.Comments)
                    .ThenInclude(c => c.Replies.Where(r => r.IsApproved))
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }

        public async Task<IEnumerable<Blog>> GetPublishedBlogsAsync()
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Where(b => b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetBlogsByCategoryAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Where(b => b.BlogCategories.Any(c => c.Id == categoryId) &&
                           b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetBlogsByTagAsync(string tag)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Where(b => b.BlogTags.Any(t => t.Slug == tag || t.Name == tag) &&
                           b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Where(b => b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetPopularBlogsAsync(int count)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Where(b => b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetPublishedBlogsAsync();

            var term = searchTerm.ToLower();
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Where(b => (b.Title.ToLower().Contains(term) ||
                           b.Content.ToLower().Contains(term) ||
                           b.Excerpt.ToLower().Contains(term)) &&
                           b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted)
                .OrderByDescending(b => b.PublishedDate ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalBlogCountAsync()
        {
            return await _dbSet.CountAsync(b => !b.IsDeleted);
        }

        public async Task<int> GetPublishedBlogCountAsync()
        {
            return await _dbSet.CountAsync(b => b.IsPublished && b.Status == BlogStatus.Published && !b.IsDeleted);
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

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _dbSet.AnyAsync(b => b.Slug == slug && !b.IsDeleted);
        }

        public override async Task<IEnumerable<Blog>> GetAllAsync()
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public override async Task<Blog?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Author)
                .Include(b => b.BlogCategories)
                .Include(b => b.BlogTags)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        }
    }
}
