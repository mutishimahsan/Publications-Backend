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
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // Assign repositories - concrete classes that implement interfaces
            Users = new UserRepository(_context);
            Products = new ProductRepository(_context);
            Categories = new CategoryRepository(_context);
            Orders = new OrderRepository(_context);
            Payments = new PaymentRepository(_context);
            Invoices = new InvoiceRepository(_context);
            Authors = new AuthorRepository(_context);
            Blogs = new BlogRepository(_context);
            Addresses = new AddressRepository(_context);
            DigitalAccesses = new DigitalAccessRepository(_context);
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
        public IAddressRepository Addresses { get; }
        public IDigitalAccessRepository DigitalAccesses { get; }

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