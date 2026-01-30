using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class BlogTagService : IBlogTagService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BlogTagService> _logger;

        public BlogTagService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BlogTagService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<BlogTagDto>> GetAllTagsAsync()
        {
            var tags = await _unitOfWork.BlogTags.GetAllAsync();
            return _mapper.Map<IEnumerable<BlogTagDto>>(tags);
        }

        public async Task<BlogTagDto?> GetTagByIdAsync(Guid id)
        {
            var tag = await _unitOfWork.BlogTags.GetByIdAsync(id);
            return tag != null ? _mapper.Map<BlogTagDto>(tag) : null;
        }

        public async Task<BlogTagDto?> GetTagBySlugAsync(string slug)
        {
            var tag = await _unitOfWork.BlogTags.GetBySlugAsync(slug);
            return tag != null ? _mapper.Map<BlogTagDto>(tag) : null;
        }

        public async Task<BlogTagDto> CreateTagAsync(CreateBlogTagDto createTagDto)
        {
            try
            {
                if (await _unitOfWork.BlogTags.ExistsByNameAsync(createTagDto.Name))
                    throw new InvalidOperationException($"Tag with name '{createTagDto.Name}' already exists");

                var tag = _mapper.Map<BlogTag>(createTagDto);
                tag.Slug = GenerateSlug(createTagDto.Name);

                var createdTag = await _unitOfWork.BlogTags.AddAsync(tag);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogTagDto>(createdTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog tag");
                throw;
            }
        }

        public async Task<BlogTagDto> UpdateTagAsync(Guid id, UpdateBlogTagDto updateTagDto)
        {
            try
            {
                var tag = await _unitOfWork.BlogTags.GetByIdAsync(id);
                if (tag == null)
                    throw new KeyNotFoundException($"Tag with ID {id} not found");

                _mapper.Map(updateTagDto, tag);
                tag.Slug = GenerateSlug(updateTagDto.Name);

                await _unitOfWork.BlogTags.UpdateAsync(tag);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogTagDto>(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog tag with ID {TagId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTagAsync(Guid id)
        {
            try
            {
                var tag = await _unitOfWork.BlogTags.GetByIdAsync(id);
                if (tag == null)
                    return false;

                tag.IsDeleted = true;
                await _unitOfWork.BlogTags.UpdateAsync(tag);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog tag with ID {TagId}", id);
                return false;
            }
        }

        public async Task<bool> TagExistsAsync(string name)
        {
            return await _unitOfWork.BlogTags.ExistsByNameAsync(name);
        }

        public async Task<IEnumerable<BlogTagDto>> GetTagsByBlogIdAsync(Guid blogId)
        {
            var tags = await _unitOfWork.BlogTags.GetByBlogIdAsync(blogId);
            return _mapper.Map<IEnumerable<BlogTagDto>>(tags);
        }

        private string GenerateSlug(string name)
        {
            return name.ToLower()
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
