using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogCommentRepository : IRepository<BlogComment>
    {
        Task<IEnumerable<BlogComment>> GetByBlogIdAsync(Guid blogId, bool includeReplies = false);
        Task<IEnumerable<BlogComment>> GetPendingCommentsAsync();
        Task<IEnumerable<BlogComment>> GetApprovedCommentsByBlogIdAsync(Guid blogId);
        Task<int> GetCommentCountByBlogIdAsync(Guid blogId);
    }
}
