using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IProductRepository : IRepository<Domain.Entities.Product>
    {
        Task<IEnumerable<Domain.Entities.Product>> GetByCategoryAsync(Guid categoryId);
        Task<IEnumerable<Domain.Entities.Product>> GetFeaturedAsync(int count = 10);
        Task<IEnumerable<Domain.Entities.Product>> SearchAsync(string searchTerm);
        Task<IEnumerable<Domain.Entities.Product>> GetByStatusAsync(Domain.Enums.ProductStatus status);
        Task UpdateStockAsync(Guid productId, int quantity);
        Task<bool> IsInStockAsync(Guid productId, int requestedQuantity);
        Task<IEnumerable<Domain.Entities.Product>> GetDigitalProductsForUserAsync(Guid userId);
    }
}
