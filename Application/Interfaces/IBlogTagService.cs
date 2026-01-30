using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogTagService
    {
        Task<IEnumerable<BlogTagDto>> GetAllTagsAsync();
        Task<BlogTagDto?> GetTagByIdAsync(Guid id);
        Task<BlogTagDto?> GetTagBySlugAsync(string slug);
        Task<BlogTagDto> CreateTagAsync(CreateBlogTagDto createTagDto);
        Task<BlogTagDto> UpdateTagAsync(Guid id, UpdateBlogTagDto updateTagDto);
        Task<bool> DeleteTagAsync(Guid id);
        Task<bool> TagExistsAsync(string name);
        Task<IEnumerable<BlogTagDto>> GetTagsByBlogIdAsync(Guid blogId);
    }
}
