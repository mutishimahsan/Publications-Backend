using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        // Constructor for runtime (dependency injection)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Parameterless constructor for design-time (migrations)
        public AppDbContext()
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Author> Authors { get; set; } = null!;
        public DbSet<ProductAuthor> ProductAuthors { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Blog> Blogs { get; set; } = null!;
        public DbSet<BlogCategory> BlogCategories { get; set; } = null!;
        public DbSet<BlogComment> BlogComments { get; set; } = null!;
        public DbSet<BlogTag> BlogTags { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<DigitalAccess> DigitalAccesses { get; set; } = null!;

        public async Task<int> RemoveOldAuditLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            var oldLogs = await AuditLogs
                .Where(al => al.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            AuditLogs.RemoveRange(oldLogs);
            return await SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure DigitalAccess
            builder.Entity<DigitalAccess>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.OrderItem)
                    .WithMany()
                    .HasForeignKey(e => e.OrderItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.DigitalAccesses)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CurrentToken)
                    .HasMaxLength(100);

                entity.HasIndex(e => e.CurrentToken)
                    .IsUnique()
                    .HasFilter("[CurrentToken] IS NOT NULL");

                entity.HasIndex(e => new { e.OrderItemId, e.CustomerId })
                    .IsUnique();
            });

            // Configure User
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.UserType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasMany(u => u.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.Addresses)
                    .WithOne(a => a.User)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // Keep cascade for addresses

                entity.HasMany(u => u.Payments)
                    .WithOne(p => p.Customer)
                    .HasForeignKey(p => p.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Product
            builder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(p => p.Description)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(p => p.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.DiscountPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.Format)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(p => p.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(p => p.ProductAuthors)
                    .WithOne(pa => pa.Product)
                    .HasForeignKey(pa => pa.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(p => !p.IsDeleted);
            });

            // Configure Category
            builder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.Slug)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // Configure Order
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(o => o.Subtotal)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.TaxAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.DiscountAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(o => o.PaymentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(o => o.FulfillmentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // FIX: Change from SetNull to Restrict or NoAction
                entity.HasOne(o => o.ShippingAddress)
                    .WithMany()
                    .HasForeignKey(o => o.ShippingAddressId)
                    .OnDelete(DeleteBehavior.NoAction); // Changed from SetNull

                entity.HasOne(o => o.BillingAddress)
                    .WithMany()
                    .HasForeignKey(o => o.BillingAddressId)
                    .OnDelete(DeleteBehavior.NoAction); // Changed from SetNull

                entity.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(o => o.Payments)
                    .WithOne(p => p.Order)
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(o => o.Invoices)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderItem
            builder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(oi => oi.DiscountPrice)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment
            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.PaymentReference)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(p => p.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.Currency)
                    .HasMaxLength(10)
                    .HasDefaultValue("PKR");

                entity.Property(p => p.Method)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(p => p.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            // Configure Invoice
            builder.Entity<Invoice>(entity =>
            {
                entity.Property(i => i.InvoiceNumber)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(i => i.Subtotal)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.TaxAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            // Configure Author
            builder.Entity<Author>(entity =>
            {
                entity.Property(a => a.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasMany(a => a.ProductAuthors)
                    .WithOne(pa => pa.Author)
                    .HasForeignKey(pa => pa.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductAuthor (junction table)
            builder.Entity<ProductAuthor>(entity =>
            {
                entity.HasKey(pa => new { pa.ProductId, pa.AuthorId });

                entity.Property(pa => pa.Role)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            // Configure Address
            builder.Entity<Address>(entity =>
            {
                entity.Property(a => a.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(a => a.AddressLine1)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(a => a.Country)
                    .HasMaxLength(100)
                    .HasDefaultValue("Pakistan");
            });

            // Configure Blog
            builder.Entity<Blog>(entity =>
            {
                entity.Property(b => b.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(b => b.Slug)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasMany(b => b.BlogCategories)
                    .WithMany(bc => bc.Blogs)
                    .UsingEntity<Dictionary<string, object>>(
                        "BlogPostCategory",
                        j => j.HasOne<BlogCategory>().WithMany().HasForeignKey("BlogCategoryId"),
                        j => j.HasOne<Blog>().WithMany().HasForeignKey("BlogId")
                    );

                entity.HasQueryFilter(b => !b.IsDeleted);
            });

            // Configure BlogCategory
            builder.Entity<BlogCategory>(entity =>
            {
                entity.Property(bc => bc.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(bc => bc.Slug)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            // Configure AuditLog
            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(a => a.TableName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(a => a.Action)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(a => a.UserId)
                    .IsRequired();

                entity.Property(a => a.OldValues)
                    .HasColumnType("nvarchar(max)");

                entity.Property(a => a.NewValues)
                    .HasColumnType("nvarchar(max)");
            });

            // REMOVED: SeedData(builder) call
        }

        // Override OnConfiguring for design-time
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This is used for migrations/design-time
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS; Database=MENTISERA_Publications; Trusted_Connection=true; TrustServerCertificate=true; MultipleActiveResultSets=true;");
            }
        }
    }
}