using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(c => c.Products.Where(p => !p.IsDeleted))
                .Include(c => c.SubCategories.Where(sc => !sc.IsDeleted))
                .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted);
        }

        public async Task<IEnumerable<Category>> GetParentCategoriesAsync()
        {
            return await _dbSet
                .Include(c => c.SubCategories.Where(sc => !sc.IsDeleted))
                .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentCategoryId)
        {
            return await _dbSet
                .Include(c => c.Products.Where(p => !p.IsDeleted))
                .Where(c => c.ParentCategoryId == parentCategoryId && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _dbSet
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories.Where(sc => !sc.IsDeleted))
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public override async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories.Where(sc => !sc.IsDeleted))
                .Include(c => c.Products.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }
    }
}
