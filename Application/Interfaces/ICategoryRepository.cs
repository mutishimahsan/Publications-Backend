using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetBySlugAsync(string slug);
        Task<IEnumerable<Category>> GetParentCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentCategoryId);
    }
}
