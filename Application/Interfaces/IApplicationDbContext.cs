using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // User related
        DbSet<User> Users { get; }

        // Product related
        DbSet<Product> Products { get; }
        DbSet<Category> Categories { get; }
        DbSet<Author> Authors { get; }
        DbSet<ProductAuthor> ProductAuthors { get; }

        // Order related
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Invoice> Invoices { get; }

        // Blog related
        DbSet<Blog> Blogs { get; }
        DbSet<BlogCategory> BlogCategories { get; }
        DbSet<BlogComment> BlogComments { get; }
        DbSet<BlogTag> BlogTags { get; }

        // Other entities
        DbSet<Address> Addresses { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<DigitalAccess> DigitalAccesses { get; }

        // Database access
        DatabaseFacade Database { get; }

        // Audit log methods
        Task<int> RemoveOldAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

        // Save changes
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}