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
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetUsersByTypeAsync(UserType userType)
        {
            return await _dbSet
                .Where(u => u.UserType == userType && u.IsActive)
                .ToListAsync();
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await UpdateAsync(user);
            }
        }
    }
}
