using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;

            // Assign repositories
            Users = new UserRepository(_context);
            Products = new ProductRepository(_context);
            Categories = new CategoryRepository(_context);
            Orders = new OrderRepository(_context);
            Payments = new PaymentRepository(_context);
            Invoices = new InvoiceRepository(_context);
            Authors = new AuthorRepository(_context);
            Blogs = new BlogRepository(_context);
            BlogCategories = new BlogCategoryRepository(_context);
            BlogTags = new BlogTagRepository(_context);
            BlogComments = new BlogCommentRepository(_context);
            Addresses = new AddressRepository(_context);
            DigitalAccesses = new DigitalAccessRepository(_context);
            EmailService = _emailService; // Assign email service
        }

        // Interface properties
        public IUserRepository Users { get; }
        public IProductRepository Products { get; }
        public ICategoryRepository Categories { get; }
        public IOrderRepository Orders { get; }
        public IPaymentRepository Payments { get; }
        public IInvoiceRepository Invoices { get; }
        public IAuthorRepository Authors { get; }
        public IBlogRepository Blogs { get; }
        public IBlogCategoryRepository BlogCategories { get; }
        public IBlogTagRepository BlogTags { get; }
        public IBlogCommentRepository BlogComments { get; }
        public IAddressRepository Addresses { get; }
        public IDigitalAccessRepository DigitalAccesses { get; }
        public IEmailService EmailService { get; } // Add this

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
            return _currentTransaction;
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}