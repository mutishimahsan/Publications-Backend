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
    public class AuthorRepository : Repository<Author>, IAuthorRepository
    {
        public AuthorRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Author?> GetByFullNameAsync(string fullName)
        {
            return await _dbSet
                .Include(a => a.ProductAuthors)
                    .ThenInclude(pa => pa.Product)
                .FirstOrDefaultAsync(a => a.FullName.ToLower() == fullName.ToLower() && !a.IsDeleted);
        }

        public async Task<IEnumerable<Author>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var term = searchTerm.ToLower();
            return await _dbSet
                .Include(a => a.ProductAuthors)
                .Where(a => !a.IsDeleted &&
                           (a.FullName.ToLower().Contains(term) ||
                            a.Affiliation != null && a.Affiliation.ToLower().Contains(term) ||
                            a.Bio != null && a.Bio.ToLower().Contains(term)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Author>> GetAuthorsWithProductsAsync()
        {
            return await _dbSet
                .Include(a => a.ProductAuthors)
                    .ThenInclude(pa => pa.Product)
                        .ThenInclude(p => p.Category)
                .Where(a => !a.IsDeleted && a.ProductAuthors.Any())
                .ToListAsync();
        }

        public override async Task<IEnumerable<Author>> GetAllAsync()
        {
            return await _dbSet
                .Include(a => a.ProductAuthors)
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.FullName)
                .ToListAsync();
        }

        public override async Task<Author?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(a => a.ProductAuthors)
                    .ThenInclude(pa => pa.Product)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }
    }
}
