using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBlogService
    {
        // Blog operations
        Task<IEnumerable<BlogDto>> GetAllBlogsAsync();
        Task<IEnumerable<BlogDto>> GetPublishedBlogsAsync();
        Task<BlogDto?> GetBlogByIdAsync(Guid id);
        Task<BlogDto?> GetBlogBySlugAsync(string slug);
        Task<BlogDto> CreateBlogAsync(CreateBlogDto createBlogDto);
        Task<BlogDto> UpdateBlogAsync(Guid id, UpdateBlogDto updateBlogDto);
        Task<bool> DeleteBlogAsync(Guid id);
        Task<IEnumerable<BlogDto>> SearchBlogsAsync(string searchTerm);
        Task IncrementViewCountAsync(Guid blogId);
        Task<IEnumerable<BlogDto>> GetBlogsByCategoryAsync(Guid categoryId);

        // New blog methods
        Task<IEnumerable<BlogDto>> GetBlogsByTagAsync(string tag);
        Task<IEnumerable<BlogDto>> GetRecentBlogsAsync(int count = 5);
        Task<IEnumerable<BlogDto>> GetPopularBlogsAsync(int count = 5);
        Task<int> GetTotalBlogCountAsync();
        Task<int> GetPublishedBlogCountAsync();
        Task<bool> SlugExistsAsync(string slug);

        // Category operations
        Task<IEnumerable<BlogCategoryDto>> GetAllCategoriesAsync();
        Task<BlogCategoryDto?> GetCategoryByIdAsync(Guid id);
        Task<BlogCategoryDto?> GetCategoryBySlugAsync(string slug);
        Task<BlogCategoryDto> CreateCategoryAsync(CreateBlogCategoryDto createCategoryDto);
        Task<BlogCategoryDto> UpdateCategoryAsync(Guid id, UpdateBlogCategoryDto updateCategoryDto);
        Task<bool> DeleteCategoryAsync(Guid id);

        // Comment operations
        Task<IEnumerable<BlogCommentDto>> GetCommentsByBlogIdAsync(Guid blogId);
        Task<BlogCommentDto?> GetCommentByIdAsync(Guid id);
        Task<BlogCommentDto> CreateCommentAsync(CreateBlogCommentDto createCommentDto);
        Task<bool> UpdateCommentStatusAsync(Guid id, CommentStatus status);
        Task<bool> DeleteCommentAsync(Guid id);
    }
}
