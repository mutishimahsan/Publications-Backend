using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IBlogTagService _blogTagService;
        private readonly ILogger<BlogController> _logger;

        public BlogController(
            IBlogService blogService,
            IBlogTagService blogTagService,
            ILogger<BlogController> logger)
        {
            _blogService = blogService;
            _blogTagService = blogTagService;
            _logger = logger;
        }

        #region Blog Endpoints

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetAllBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllBlogsAsync();
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all blogs");
                return StatusCode(500, "An error occurred while retrieving blogs");
            }
        }

        [HttpGet("published")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetPublishedBlogs()
        {
            try
            {
                var blogs = await _blogService.GetPublishedBlogsAsync();
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published blogs");
                return StatusCode(500, "An error occurred while retrieving published blogs");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BlogDto>> GetBlogById(Guid id)
        {
            try
            {
                var blog = await _blogService.GetBlogByIdAsync(id);
                if (blog == null)
                    return NotFound($"Blog with ID {id} not found");

                return Ok(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog with ID {BlogId}", id);
                return StatusCode(500, "An error occurred while retrieving the blog");
            }
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<BlogDto>> GetBlogBySlug(string slug)
        {
            try
            {
                var blog = await _blogService.GetBlogBySlugAsync(slug);
                if (blog == null)
                    return NotFound($"Blog with slug '{slug}' not found");

                // Increment view count asynchronously
                _ = Task.Run(async () =>
                {
                    await _blogService.IncrementViewCountAsync(blog.Id);
                });

                return Ok(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog with slug {Slug}", slug);
                return StatusCode(500, "An error occurred while retrieving the blog");
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogDto>> CreateBlog([FromBody] CreateBlogDto createBlogDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var blog = await _blogService.CreateBlogAsync(createBlogDto);
                return CreatedAtAction(nameof(GetBlogById), new { id = blog.Id }, blog);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog");
                return StatusCode(500, "An error occurred while creating the blog");
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogDto>> UpdateBlog(Guid id, [FromBody] UpdateBlogDto updateBlogDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var blog = await _blogService.UpdateBlogAsync(id, updateBlogDto);
                return Ok(blog);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog with ID {BlogId}", id);
                return StatusCode(500, "An error occurred while updating the blog");
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<IActionResult> DeleteBlog(Guid id)
        {
            try
            {
                var result = await _blogService.DeleteBlogAsync(id);
                if (!result)
                    return NotFound($"Blog with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog with ID {BlogId}", id);
                return StatusCode(500, "An error occurred while deleting the blog");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> SearchBlogs([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return BadRequest("Search term is required");

                var blogs = await _blogService.SearchBlogsAsync(q);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching blogs with term {SearchTerm}", q);
                return StatusCode(500, "An error occurred while searching blogs");
            }
        }

        [HttpGet("category/{categoryId:guid}")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetBlogsByCategory(Guid categoryId)
        {
            try
            {
                var blogs = await _blogService.GetBlogsByCategoryAsync(categoryId);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blogs by category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while retrieving blogs by category");
            }
        }

        [HttpGet("tag/{tag}")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetBlogsByTag(string tag)
        {
            try
            {
                var blogs = await _blogService.GetBlogsByTagAsync(tag);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blogs by tag {Tag}", tag);
                return StatusCode(500, "An error occurred while retrieving blogs by tag");
            }
        }

        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetRecentBlogs([FromQuery] int count = 5)
        {
            try
            {
                var blogs = await _blogService.GetRecentBlogsAsync(count);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent blogs");
                return StatusCode(500, "An error occurred while retrieving recent blogs");
            }
        }

        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<BlogDto>>> GetPopularBlogs([FromQuery] int count = 5)
        {
            try
            {
                var blogs = await _blogService.GetPopularBlogsAsync(count);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular blogs");
                return StatusCode(500, "An error occurred while retrieving popular blogs");
            }
        }

        [HttpGet("stats/count")]
        public async Task<ActionResult<object>> GetBlogStats()
        {
            try
            {
                var totalCount = await _blogService.GetTotalBlogCountAsync();
                var publishedCount = await _blogService.GetPublishedBlogCountAsync();

                return Ok(new
                {
                    TotalBlogs = totalCount,
                    PublishedBlogs = publishedCount,
                    DraftBlogs = totalCount - publishedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog stats");
                return StatusCode(500, "An error occurred while retrieving blog statistics");
            }
        }

        [HttpGet("check-slug")]
        public async Task<ActionResult<bool>> CheckSlugExists([FromQuery] string slug)
        {
            try
            {
                var exists = await _blogService.SlugExistsAsync(slug);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slug {Slug}", slug);
                return StatusCode(500, "An error occurred while checking slug");
            }
        }

        #endregion

        #region Category Endpoints

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<BlogCategoryDto>>> GetAllCategories()
        {
            try
            {
                var categories = await _blogService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return StatusCode(500, "An error occurred while retrieving categories");
            }
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<ActionResult<BlogCategoryDto>> GetCategoryById(Guid id)
        {
            try
            {
                var category = await _blogService.GetCategoryByIdAsync(id);
                if (category == null)
                    return NotFound($"Category with ID {id} not found");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category with ID {CategoryId}", id);
                return StatusCode(500, "An error occurred while retrieving the category");
            }
        }

        [HttpGet("categories/slug/{slug}")]
        public async Task<ActionResult<BlogCategoryDto>> GetCategoryBySlug(string slug)
        {
            try
            {
                var category = await _blogService.GetCategoryBySlugAsync(slug);
                if (category == null)
                    return NotFound($"Category with slug '{slug}' not found");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category with slug {Slug}", slug);
                return StatusCode(500, "An error occurred while retrieving the category");
            }
        }

        [HttpPost("categories")]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogCategoryDto>> CreateCategory([FromBody] CreateBlogCategoryDto createCategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var category = await _blogService.CreateCategoryAsync(createCategoryDto);
                return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, "An error occurred while creating the category");
            }
        }

        [HttpPut("categories/{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogCategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateBlogCategoryDto updateCategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var category = await _blogService.UpdateCategoryAsync(id, updateCategoryDto);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {CategoryId}", id);
                return StatusCode(500, "An error occurred while updating the category");
            }
        }

        [HttpDelete("categories/{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var result = await _blogService.DeleteCategoryAsync(id);
                if (!result)
                    return NotFound($"Category with ID {id} not found");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
                return StatusCode(500, "An error occurred while deleting the category");
            }
        }

        #endregion

        #region Comment Endpoints

        [HttpGet("{blogId:guid}/comments")]
        public async Task<ActionResult<IEnumerable<BlogCommentDto>>> GetCommentsByBlogId(Guid blogId)
        {
            try
            {
                var comments = await _blogService.GetCommentsByBlogIdAsync(blogId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for blog {BlogId}", blogId);
                return StatusCode(500, "An error occurred while retrieving comments");
            }
        }

        [HttpGet("comments/{id:guid}")]
        public async Task<ActionResult<BlogCommentDto>> GetCommentById(Guid id)
        {
            try
            {
                var comment = await _blogService.GetCommentByIdAsync(id);
                if (comment == null)
                    return NotFound($"Comment with ID {id} not found");

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment with ID {CommentId}", id);
                return StatusCode(500, "An error occurred while retrieving the comment");
            }
        }

        [HttpPost("comments")]
        public async Task<ActionResult<BlogCommentDto>> CreateComment([FromBody] CreateBlogCommentDto createCommentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var comment = await _blogService.CreateCommentAsync(createCommentDto);
                return CreatedAtAction(nameof(GetCommentById), new { id = comment.Id }, comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, "An error occurred while creating the comment");
            }
        }

        [HttpPut("comments/{id:guid}/status")]
        [Authorize(Policy = "RequireContent")]
        public async Task<IActionResult> UpdateCommentStatus(Guid id, [FromBody] UpdateCommentStatusRequest request)
        {
            try
            {
                var result = await _blogService.UpdateCommentStatusAsync(id, request.Status);
                if (!result)
                    return NotFound($"Comment with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment status for ID {CommentId}", id);
                return StatusCode(500, "An error occurred while updating comment status");
            }
        }

        [HttpDelete("comments/{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            try
            {
                var result = await _blogService.DeleteCommentAsync(id);
                if (!result)
                    return NotFound($"Comment with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment with ID {CommentId}", id);
                return StatusCode(500, "An error occurred while deleting the comment");
            }
        }

        #endregion

        #region Tag Endpoints

        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<BlogTagDto>>> GetAllTags()
        {
            try
            {
                var tags = await _blogTagService.GetAllTagsAsync();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tags");
                return StatusCode(500, "An error occurred while retrieving tags");
            }
        }

        [HttpGet("tags/{id:guid}")]
        public async Task<ActionResult<BlogTagDto>> GetTagById(Guid id)
        {
            try
            {
                var tag = await _blogTagService.GetTagByIdAsync(id);
                if (tag == null)
                    return NotFound($"Tag with ID {id} not found");

                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag with ID {TagId}", id);
                return StatusCode(500, "An error occurred while retrieving the tag");
            }
        }

        [HttpGet("tags/slug/{slug}")]
        public async Task<ActionResult<BlogTagDto>> GetTagBySlug(string slug)
        {
            try
            {
                var tag = await _blogTagService.GetTagBySlugAsync(slug);
                if (tag == null)
                    return NotFound($"Tag with slug '{slug}' not found");

                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag with slug {Slug}", slug);
                return StatusCode(500, "An error occurred while retrieving the tag");
            }
        }

        [HttpGet("tags/blog/{blogId:guid}")]
        public async Task<ActionResult<IEnumerable<BlogTagDto>>> GetTagsByBlogId(Guid blogId)
        {
            try
            {
                var tags = await _blogTagService.GetTagsByBlogIdAsync(blogId);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for blog {BlogId}", blogId);
                return StatusCode(500, "An error occurred while retrieving tags");
            }
        }

        [HttpPost("tags")]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogTagDto>> CreateTag([FromBody] CreateBlogTagDto createTagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tag = await _blogTagService.CreateTagAsync(createTagDto);
                return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, tag);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, "An error occurred while creating the tag");
            }
        }

        [HttpPut("tags/{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<ActionResult<BlogTagDto>> UpdateTag(Guid id, [FromBody] UpdateBlogTagDto updateTagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tag = await _blogTagService.UpdateTagAsync(id, updateTagDto);
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag with ID {TagId}", id);
                return StatusCode(500, "An error occurred while updating the tag");
            }
        }

        [HttpDelete("tags/{id:guid}")]
        [Authorize(Policy = "RequireContent")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            try
            {
                var result = await _blogTagService.DeleteTagAsync(id);
                if (!result)
                    return NotFound($"Tag with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag with ID {TagId}", id);
                return StatusCode(500, "An error occurred while deleting the tag");
            }
        }

        [HttpGet("tags/check-name")]
        public async Task<ActionResult<bool>> CheckTagExists([FromQuery] string name)
        {
            try
            {
                var exists = await _blogTagService.TagExistsAsync(name);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking tag name {Name}", name);
                return StatusCode(500, "An error occurred while checking tag name");
            }
        }

        #endregion
    }

    public class UpdateCommentStatusRequest
    {
        [Required]
        public CommentStatus Status { get; set; }
    }
}

