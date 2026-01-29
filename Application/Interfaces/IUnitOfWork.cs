using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IOrderRepository Orders { get; }
        IPaymentRepository Payments { get; }
        IInvoiceRepository Invoices { get; }
        IAuthorRepository Authors { get; }
        IBlogRepository Blogs { get; }
        IBlogCategoryRepository BlogCategories { get; }
        IBlogTagRepository BlogTags { get; }
        IBlogCommentRepository BlogComments { get; }
        IAddressRepository Addresses { get; }
        IDigitalAccessRepository DigitalAccesses { get; }
        IEmailService EmailService { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
