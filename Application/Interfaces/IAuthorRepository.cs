using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAuthorRepository : IRepository<Author>
    {
        Task<Author?> GetByFullNameAsync(string fullName);
        Task<IEnumerable<Author>> SearchAsync(string searchTerm);
        Task<IEnumerable<Author>> GetAuthorsWithProductsAsync();
    }
}
