using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserRepository : IRepository<Domain.Entities.User>
    {
        Task<Domain.Entities.User?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<IEnumerable<Domain.Entities.User>> GetUsersByTypeAsync(Domain.Enums.UserType userType);
        Task UpdateLastLoginAsync(Guid userId);
    }
}
