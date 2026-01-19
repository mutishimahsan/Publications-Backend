using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ICategoryService
    {
        Task<CategoryDto> GetCategoryAsync(Guid id);
        Task<CategoryDto> GetCategoryBySlugAsync(string slug);
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetParentCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentCategoryId);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, string createdBy);
        Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, string updatedBy);
        Task<bool> DeleteCategoryAsync(Guid id);
        Task<bool> ReorderCategoriesAsync(List<CategoryReorderDto> reorderList);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICategoryRepository categoryRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryDto> GetCategoryAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException("Category", id);
            }

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> GetCategoryBySlugAsync(string slug)
        {
            var category = await _categoryRepository.GetBySlugAsync(slug);

            if (category == null)
            {
                throw new NotFoundException("Category", slug);
            }

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<IEnumerable<CategoryDto>> GetParentCategoriesAsync()
        {
            var categories = await _categoryRepository.GetParentCategoriesAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentCategoryId)
        {
            var categories = await _categoryRepository.GetSubCategoriesAsync(parentCategoryId);
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, string createdBy)
        {
            // Validate parent category
            if (dto.ParentCategoryId.HasValue)
            {
                var parentCategory = await _categoryRepository.GetByIdAsync(dto.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    throw new ValidationException("Parent category not found.");
                }
            }

            // Check if slug exists
            var existingCategory = await _categoryRepository.GetBySlugAsync(dto.Slug);
            if (existingCategory != null)
            {
                throw new ValidationException($"Slug '{dto.Slug}' is already in use.");
            }

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug,
                DisplayOrder = dto.DisplayOrder,
                ParentCategoryId = dto.ParentCategoryId,
                CreatedBy = createdBy
            };

            await _categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return await GetCategoryAsync(category.Id);
        }

        public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto, string updatedBy)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException("Category", id);
            }

            // Prevent circular reference
            if (dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value == id)
            {
                throw new ValidationException("Category cannot be its own parent.");
            }

            // Validate parent category
            if (dto.ParentCategoryId.HasValue)
            {
                var parentCategory = await _categoryRepository.GetByIdAsync(dto.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    throw new ValidationException("Parent category not found.");
                }

                // Check for circular reference in hierarchy
                if (IsCircularReference(category, parentCategory))
                {
                    throw new ValidationException("Circular reference detected in category hierarchy.");
                }
            }

            // Check if slug exists (if changing)
            if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != category.Slug)
            {
                var existingCategory = await _categoryRepository.GetBySlugAsync(dto.Slug);
                if (existingCategory != null && existingCategory.Id != id)
                {
                    throw new ValidationException($"Slug '{dto.Slug}' is already in use.");
                }
                category.Slug = dto.Slug;
            }

            // Update fields
            if (!string.IsNullOrEmpty(dto.Name))
                category.Name = dto.Name;

            if (dto.Description != null)
                category.Description = dto.Description;

            if (dto.DisplayOrder.HasValue)
                category.DisplayOrder = dto.DisplayOrder.Value;

            if (dto.ParentCategoryId.HasValue)
                category.ParentCategoryId = dto.ParentCategoryId.Value;

            category.UpdatedBy = updatedBy;

            await _categoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return await GetCategoryAsync(category.Id);
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException("Category", id);
            }

            // Check if category has subcategories
            if (category.SubCategories.Any())
            {
                throw new ValidationException("Cannot delete category with subcategories. Please delete subcategories first.");
            }

            // Check if category has products
            if (category.Products.Any())
            {
                throw new ValidationException("Cannot delete category with products. Please reassign or delete products first.");
            }

            // Soft delete
            category.IsDeleted = true;
            await _categoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReorderCategoriesAsync(List<CategoryReorderDto> reorderList)
        {
            foreach (var item in reorderList)
            {
                var category = await _categoryRepository.GetByIdAsync(item.CategoryId);
                if (category != null)
                {
                    category.DisplayOrder = item.DisplayOrder;
                    await _categoryRepository.UpdateAsync(category);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private bool IsCircularReference(Category category, Category potentialParent)
        {
            var current = potentialParent;
            while (current != null)
            {
                if (current.Id == category.Id)
                    return true;

                current = current.ParentCategory;
            }
            return false;
        }
    }

    public class CategoryReorderDto
    {
        public Guid CategoryId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
