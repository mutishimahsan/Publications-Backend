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
    public class BlogCommentRepository : Repository<BlogComment>, IBlogCommentRepository
    {
        public BlogCommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BlogComment>> GetByBlogIdAsync(Guid blogId, bool includeReplies = false)
        {
            var query = _dbSet
                .Where(c => c.BlogId == blogId && !c.IsDeleted && c.ParentCommentId == null);

            if (includeReplies)
            {
                query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogComment>> GetPendingCommentsAsync()
        {
            return await _dbSet
                .Include(c => c.Blog)
                .Where(c => !c.IsDeleted && c.Status == CommentStatus.Pending)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogComment>> GetApprovedCommentsByBlogIdAsync(Guid blogId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted &&
                           c.BlogId == blogId &&
                           c.Status == CommentStatus.Approved &&
                           c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCommentCountByBlogIdAsync(Guid blogId)
        {
            return await _dbSet
                .CountAsync(c => c.BlogId == blogId &&
                                !c.IsDeleted &&
                                c.Status == CommentStatus.Approved);
        }

        // Fix: Override GetByIdAsync to include necessary data
        public override async Task<BlogComment?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Blog)
                .Include(c => c.ParentComment)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }
    }
}
