using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Order> Orders { get; }
        DbSet<Product> Products { get; }
        DbSet<Blog> Blogs { get; }
        DbSet<Payment> Payments { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<BlogComment> BlogComments { get; }
        DbSet<Category> Categories { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<DigitalAccess> DigitalAccesses { get; }

        DatabaseFacade Database { get; }
        Task<int> RemoveOldAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
