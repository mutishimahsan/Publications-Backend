using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BlogService> _logger;

        public BlogService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BlogService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // Blog operations
        public async Task<IEnumerable<BlogDto>> GetAllBlogsAsync()
        {
            var blogs = await _unitOfWork.Blogs.GetAllAsync();
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task<IEnumerable<BlogDto>> GetPublishedBlogsAsync()
        {
            var blogs = await _unitOfWork.Blogs.GetPublishedBlogsAsync();
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task<BlogDto?> GetBlogByIdAsync(Guid id)
        {
            var blog = await _unitOfWork.Blogs.GetByIdAsync(id);
            return blog != null ? _mapper.Map<BlogDto>(blog) : null;
        }

        public async Task<BlogDto?> GetBlogBySlugAsync(string slug)
        {
            var blog = await _unitOfWork.Blogs.GetBySlugAsync(slug);
            return blog != null ? _mapper.Map<BlogDto>(blog) : null;
        }

        public async Task<BlogDto> CreateBlogAsync(CreateBlogDto createBlogDto)
        {
            try
            {
                var blog = _mapper.Map<Blog>(createBlogDto);
                blog.Slug = GenerateSlug(createBlogDto.Title);
                blog.Status = createBlogDto.IsPublished ? BlogStatus.Published : BlogStatus.Draft;

                if (createBlogDto.IsPublished)
                {
                    blog.PublishedDate = DateTime.UtcNow;
                }

                // Add categories
                foreach (var categoryId in createBlogDto.CategoryIds)
                {
                    var category = await _unitOfWork.BlogCategories.GetByIdAsync(categoryId);
                    if (category != null)
                    {
                        blog.BlogCategories.Add(category);
                    }
                }

                // Add tags if provided
                if (createBlogDto.TagIds != null && createBlogDto.TagIds.Any())
                {
                    foreach (var tagId in createBlogDto.TagIds)
                    {
                        var tag = await _unitOfWork.BlogTags.GetByIdAsync(tagId);
                        if (tag != null)
                        {
                            blog.BlogTags.Add(tag);
                        }
                    }
                }

                var createdBlog = await _unitOfWork.Blogs.AddAsync(blog);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogDto>(createdBlog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog");
                throw;
            }
        }

        public async Task<BlogDto> UpdateBlogAsync(Guid id, UpdateBlogDto updateBlogDto)
        {
            try
            {
                var blog = await _unitOfWork.Blogs.GetByIdAsync(id);
                if (blog == null)
                    throw new KeyNotFoundException($"Blog with ID {id} not found");

                _mapper.Map(updateBlogDto, blog);
                blog.Slug = GenerateSlug(updateBlogDto.Title);
                blog.Status = updateBlogDto.IsPublished ? BlogStatus.Published : BlogStatus.Draft;

                // Update categories
                blog.BlogCategories.Clear();
                foreach (var categoryId in updateBlogDto.CategoryIds)
                {
                    var category = await _unitOfWork.BlogCategories.GetByIdAsync(categoryId);
                    if (category != null)
                    {
                        blog.BlogCategories.Add(category);
                    }
                }

                // Update tags
                blog.BlogTags.Clear();
                if (updateBlogDto.TagIds != null && updateBlogDto.TagIds.Any())
                {
                    foreach (var tagId in updateBlogDto.TagIds)
                    {
                        var tag = await _unitOfWork.BlogTags.GetByIdAsync(tagId);
                        if (tag != null)
                        {
                            blog.BlogTags.Add(tag);
                        }
                    }
                }

                if (updateBlogDto.IsPublished && !blog.IsPublished)
                {
                    blog.PublishedDate = DateTime.UtcNow;
                }
                else if (!updateBlogDto.IsPublished)
                {
                    blog.PublishedDate = null;
                }

                await _unitOfWork.Blogs.UpdateAsync(blog);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogDto>(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog with ID {BlogId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBlogAsync(Guid id)
        {
            try
            {
                var blog = await _unitOfWork.Blogs.GetByIdAsync(id);
                if (blog == null)
                    return false;

                blog.IsDeleted = true;
                await _unitOfWork.Blogs.UpdateAsync(blog);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog with ID {BlogId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<BlogDto>> SearchBlogsAsync(string searchTerm)
        {
            var blogs = await _unitOfWork.Blogs.SearchBlogsAsync(searchTerm);
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task IncrementViewCountAsync(Guid blogId)
        {
            await _unitOfWork.Blogs.IncrementViewCountAsync(blogId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<BlogDto>> GetBlogsByCategoryAsync(Guid categoryId)
        {
            var blogs = await _unitOfWork.Blogs.GetBlogsByCategoryAsync(categoryId);
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        // Add new methods from repository
        public async Task<IEnumerable<BlogDto>> GetBlogsByTagAsync(string tag)
        {
            var blogs = await _unitOfWork.Blogs.GetBlogsByTagAsync(tag);
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task<IEnumerable<BlogDto>> GetRecentBlogsAsync(int count = 5)
        {
            var blogs = await _unitOfWork.Blogs.GetRecentBlogsAsync(count);
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task<IEnumerable<BlogDto>> GetPopularBlogsAsync(int count = 5)
        {
            var blogs = await _unitOfWork.Blogs.GetPopularBlogsAsync(count);
            return _mapper.Map<IEnumerable<BlogDto>>(blogs);
        }

        public async Task<int> GetTotalBlogCountAsync()
        {
            return await _unitOfWork.Blogs.GetTotalBlogCountAsync();
        }

        public async Task<int> GetPublishedBlogCountAsync()
        {
            return await _unitOfWork.Blogs.GetPublishedBlogCountAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _unitOfWork.Blogs.SlugExistsAsync(slug);
        }

        // Category operations
        public async Task<IEnumerable<BlogCategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.BlogCategories.GetAllWithBlogCountAsync();
            return _mapper.Map<IEnumerable<BlogCategoryDto>>(categories);
        }

        public async Task<BlogCategoryDto?> GetCategoryByIdAsync(Guid id)
        {
            var category = await _unitOfWork.BlogCategories.GetByIdAsync(id);
            return category != null ? _mapper.Map<BlogCategoryDto>(category) : null;
        }

        public async Task<BlogCategoryDto?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _unitOfWork.BlogCategories.GetBySlugAsync(slug);
            return category != null ? _mapper.Map<BlogCategoryDto>(category) : null;
        }

        public async Task<BlogCategoryDto> CreateCategoryAsync(CreateBlogCategoryDto createCategoryDto)
        {
            try
            {
                if (await _unitOfWork.BlogCategories.ExistsByNameAsync(createCategoryDto.Name))
                    throw new InvalidOperationException($"Category with name '{createCategoryDto.Name}' already exists");

                var category = _mapper.Map<BlogCategory>(createCategoryDto);
                category.Slug = GenerateSlug(createCategoryDto.Name);

                var createdCategory = await _unitOfWork.BlogCategories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogCategoryDto>(createdCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog category");
                throw;
            }
        }

        public async Task<BlogCategoryDto> UpdateCategoryAsync(Guid id, UpdateBlogCategoryDto updateCategoryDto)
        {
            try
            {
                var category = await _unitOfWork.BlogCategories.GetByIdAsync(id);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID {id} not found");

                _mapper.Map(updateCategoryDto, category);
                category.Slug = GenerateSlug(updateCategoryDto.Name);

                await _unitOfWork.BlogCategories.UpdateAsync(category);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogCategoryDto>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog category with ID {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var category = await _unitOfWork.BlogCategories.GetByIdAsync(id);
                if (category == null)
                    return false;

                // Check if category has blogs
                if (category.Blogs.Any())
                    throw new InvalidOperationException("Cannot delete category that has associated blogs");

                category.IsDeleted = true;
                await _unitOfWork.BlogCategories.UpdateAsync(category);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog category with ID {CategoryId}", id);
                throw;
            }
        }

        // Comment operations
        public async Task<IEnumerable<BlogCommentDto>> GetCommentsByBlogIdAsync(Guid blogId)
        {
            var comments = await _unitOfWork.BlogComments.GetApprovedCommentsByBlogIdAsync(blogId);
            return _mapper.Map<IEnumerable<BlogCommentDto>>(comments);
        }

        public async Task<BlogCommentDto?> GetCommentByIdAsync(Guid id)
        {
            var comment = await _unitOfWork.BlogComments.GetByIdAsync(id);
            return comment != null ? _mapper.Map<BlogCommentDto>(comment) : null;
        }

        public async Task<BlogCommentDto> CreateCommentAsync(CreateBlogCommentDto createCommentDto)
        {
            try
            {
                var comment = _mapper.Map<BlogComment>(createCommentDto);
                comment.Status = CommentStatus.Pending;

                var createdComment = await _unitOfWork.BlogComments.AddAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogCommentDto>(createdComment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog comment");
                throw;
            }
        }

        public async Task<bool> UpdateCommentStatusAsync(Guid id, CommentStatus status)
        {
            try
            {
                var comment = await _unitOfWork.BlogComments.GetByIdAsync(id);
                if (comment == null)
                    return false;

                comment.Status = status;
                await _unitOfWork.BlogComments.UpdateAsync(comment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment status for comment ID {CommentId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(Guid id)
        {
            try
            {
                var comment = await _unitOfWork.BlogComments.GetByIdAsync(id);
                if (comment == null)
                    return false;

                comment.IsDeleted = true;
                await _unitOfWork.BlogComments.UpdateAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment with ID {CommentId}", id);
                return false;
            }
        }

        private string GenerateSlug(string title)
        {
            return title.ToLower()
                .Replace(" ", "-")
                .Replace(".", "-")
                .Replace(",", "-")
                .Replace("?", "")
                .Replace("!", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(":", "")
                .Replace(";", "");
        }
    }
}
