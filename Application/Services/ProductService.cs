using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IProductService
    {
        Task<ProductDto> GetProductAsync(Guid id);
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId);
        Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(int count = 10);
        Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
        Task<ProductDto> CreateProductAsync(CreateProductDto dto, string createdBy);
        Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string updatedBy);
        Task<bool> DeleteProductAsync(Guid id);
        Task<bool> UpdateStockAsync(Guid productId, int quantity);
        Task<bool> IsProductAvailableAsync(Guid productId, int quantity);
    }

    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;

        public ProductService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
        }

        public async Task<ProductDto> GetProductAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null || product.IsDeleted)
            {
                throw new NotFoundException("Product", id);
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products.Where(p => !p.IsDeleted));
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var products = await _unitOfWork.Products.GetByCategoryAsync(categoryId);
            return _mapper.Map<IEnumerable<ProductDto>>(products.Where(p => !p.IsDeleted));
        }

        public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(int count = 10)
        {
            var products = await _unitOfWork.Products.GetFeaturedAsync(count);
            return _mapper.Map<IEnumerable<ProductDto>>(products.Where(p => !p.IsDeleted));
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllProductsAsync();
            }

            var products = await _unitOfWork.Products.SearchAsync(searchTerm);
            return _mapper.Map<IEnumerable<ProductDto>>(products.Where(p => !p.IsDeleted));
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, string createdBy)
        {
            // Validate category
            if (dto.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                {
                    throw new ValidationException("Category not found.");
                }
            }

            // Create product
            var product = new Product
            {
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                ISBN = dto.ISBN,
                Description = dto.Description,
                TableOfContents = dto.TableOfContents,
                IntendedAudience = dto.IntendedAudience,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                StockQuantity = dto.StockQuantity,
                MaxDownloads = dto.MaxDownloads,
                Format = dto.Format,
                Type = dto.Type,
                CategoryId = dto.CategoryId,
                Status = ProductStatus.Draft,
                CreatedBy = createdBy
            };

            // Handle file uploads
            if (dto.CoverImage != null)
            {
                product.CoverImageUrl = await _fileStorageService.SaveFileAsync(dto.CoverImage, "product-images");
            }

            if (dto.SampleFile != null)
            {
                product.SampleFileUrl = await _fileStorageService.SaveFileAsync(dto.SampleFile, "sample-files");
            }

            if (dto.DigitalFile != null && (dto.Format == ProductFormat.Digital || dto.Format == ProductFormat.Bundle))
            {
                product.DigitalFileUrl = await _fileStorageService.SaveFileAsync(dto.DigitalFile, "digital-files");
            }

            // Add authors
            if (dto.AuthorIds != null && dto.AuthorIds.Any())
            {
                foreach (var authorId in dto.AuthorIds)
                {
                    var author = await _unitOfWork.Authors.GetByIdAsync(authorId);
                    if (author != null)
                    {
                        product.ProductAuthors.Add(new ProductAuthor
                        {
                            Product = product,
                            AuthorId = authorId
                        });
                    }
                }
            }

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return await GetProductAsync(product.Id);
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string updatedBy)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null || product.IsDeleted)
            {
                throw new NotFoundException("Product", id);
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(dto.Title))
                product.Title = dto.Title;

            if (dto.Subtitle != null)
                product.Subtitle = dto.Subtitle;

            if (dto.ISBN != null)
                product.ISBN = dto.ISBN;

            if (!string.IsNullOrEmpty(dto.Description))
                product.Description = dto.Description;

            if (dto.TableOfContents != null)
                product.TableOfContents = dto.TableOfContents;

            if (dto.IntendedAudience != null)
                product.IntendedAudience = dto.IntendedAudience;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            if (dto.DiscountPrice != null)
                product.DiscountPrice = dto.DiscountPrice;

            if (dto.StockQuantity.HasValue)
                product.StockQuantity = dto.StockQuantity.Value;

            if (dto.MaxDownloads != null)
                product.MaxDownloads = dto.MaxDownloads;

            if (dto.Format.HasValue)
                product.Format = dto.Format.Value;

            if (dto.Type.HasValue)
                product.Type = dto.Type.Value;

            if (dto.Status.HasValue)
                product.Status = dto.Status.Value;

            if (dto.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                {
                    throw new ValidationException("Category not found.");
                }
                product.CategoryId = dto.CategoryId.Value;
            }

            product.UpdatedBy = updatedBy;
            product.UpdatedAt = DateTime.UtcNow;

            // Handle file uploads
            if (dto.CoverImage != null)
            {
                product.CoverImageUrl = await _fileStorageService.SaveFileAsync(dto.CoverImage, "product-images");
            }

            if (dto.SampleFile != null)
            {
                product.SampleFileUrl = await _fileStorageService.SaveFileAsync(dto.SampleFile, "sample-files");
            }

            if (dto.DigitalFile != null && (product.Format == ProductFormat.Digital || product.Format == ProductFormat.Bundle))
            {
                product.DigitalFileUrl = await _fileStorageService.SaveFileAsync(dto.DigitalFile, "digital-files");
            }

            // Update authors if provided
            if (dto.AuthorIds != null)
            {
                // Remove existing authors
                var existingAuthors = product.ProductAuthors.ToList();
                foreach (var author in existingAuthors)
                {
                    // In real implementation, you'd need to remove these properly
                }

                // Add new authors
                foreach (var authorId in dto.AuthorIds)
                {
                    var author = await _unitOfWork.Authors.GetByIdAsync(authorId);
                    if (author != null)
                    {
                        product.ProductAuthors.Add(new ProductAuthor
                        {
                            Product = product,
                            AuthorId = authorId
                        });
                    }
                }
            }

            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return await GetProductAsync(product.Id);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null)
            {
                throw new NotFoundException("Product", id);
            }

            product.IsDeleted = true;
            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStockAsync(Guid productId, int quantity)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);

            if (product == null)
            {
                throw new NotFoundException("Product", productId);
            }

            product.StockQuantity += quantity;

            if (product.StockQuantity < 0)
            {
                throw new ValidationException("Insufficient stock.");
            }

            // Update status if out of stock
            if (product.StockQuantity == 0)
            {
                product.Status = ProductStatus.OutOfStock;
            }
            else if (product.Status == ProductStatus.OutOfStock)
            {
                product.Status = ProductStatus.Published;
            }

            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);

            if (product == null || product.IsDeleted || product.Status != ProductStatus.Published)
            {
                return false;
            }

            // For digital products, always available
            if (product.Format == ProductFormat.Digital || product.Format == ProductFormat.Bundle)
            {
                return true;
            }

            // For print products, check stock
            return product.StockQuantity >= quantity;
        }
    }
}
