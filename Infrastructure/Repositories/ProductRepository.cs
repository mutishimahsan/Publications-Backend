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
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.ProductAuthors)
                    .ThenInclude(pa => pa.Author)
                .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.ProductAuthors)
                    .ThenInclude(pa => pa.Author)
                .Where(p => !p.IsDeleted && p.Status == ProductStatus.Published)
                .OrderByDescending(p => p.PublishedDate ?? p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var term = searchTerm.ToLower();
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.ProductAuthors)
                    .ThenInclude(pa => pa.Author)
                .Where(p => !p.IsDeleted &&
                           (p.Title.ToLower().Contains(term) ||
                            p.Description.ToLower().Contains(term) ||
                            p.ISBN != null && p.ISBN.ToLower().Contains(term)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByStatusAsync(ProductStatus status)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted && p.Status == status)
                .ToListAsync();
        }

        public async Task UpdateStockAsync(Guid productId, int quantity)
        {
            var product = await GetByIdAsync(productId);
            if (product != null)
            {
                product.StockQuantity += quantity;
                await UpdateAsync(product);
            }
        }

        public async Task<bool> IsInStockAsync(Guid productId, int requestedQuantity)
        {
            var product = await _dbSet
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product == null || product.Status != ProductStatus.Published)
                return false;

            // Digital products are always "in stock"
            if (product.Format == ProductFormat.Digital || product.Format == ProductFormat.Bundle)
                return true;

            return product.StockQuantity >= requestedQuantity;
        }

        public async Task<IEnumerable<Product>> GetDigitalProductsForUserAsync(Guid userId)
        {
            // This would typically join with OrderItems to get products purchased by user
            return await _dbSet
                .Include(p => p.ProductAuthors)
                    .ThenInclude(pa => pa.Author)
                .Where(p => !p.IsDeleted &&
                           p.Status == ProductStatus.Published &&
                           (p.Format == ProductFormat.Digital || p.Format == ProductFormat.Bundle))
                .ToListAsync();
        }
    }
}
