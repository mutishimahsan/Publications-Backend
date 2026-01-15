using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SeedService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ILogger<SeedService> _logger;

        public SeedService(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<SeedService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                // Apply any pending migrations
                await _context.Database.MigrateAsync();

                // Seed in order
                await SeedRolesAsync();
                await SeedAdminUserAsync();
                await SeedCategoriesAsync();
                await SeedSampleProductsAsync();

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[]
            {
                "SuperAdmin",
                "FinanceAdmin",
                "ContentAdmin",
                "SupportAdmin",
                "Customer"
            };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var adminEmail = "admin@mentisera.pk";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    UserType = UserType.SuperAdmin,
                    IsActive = true,
                    EmailConfirmed = true,
                    PhoneNumber = "+923001234567",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                    _logger.LogInformation("Created admin user: {Email}", adminEmail);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create admin user: {Errors}", errors);
                }
            }
        }

        private async Task SeedCategoriesAsync()
        {
            if (!await _context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Computer Science",
                        Slug = "computer-science",
                        Description = "Books and publications related to computer science",
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Business & Management",
                        Slug = "business-management",
                        Description = "Business management and administration publications",
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Engineering",
                        Slug = "engineering",
                        Description = "Engineering textbooks and guides",
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Medical Sciences",
                        Slug = "medical-sciences",
                        Description = "Medical and healthcare publications",
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Social Sciences",
                        Slug = "social-sciences",
                        Description = "Social sciences and humanities publications",
                        DisplayOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Categories.AddRangeAsync(categories);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} categories", categories.Length);
            }
        }

        private async Task SeedSampleProductsAsync()
        {
            if (!await _context.Products.AnyAsync())
            {
                var computerScienceCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Slug == "computer-science");

                if (computerScienceCategory != null)
                {
                    var products = new[]
                    {
                        new Product
                        {
                            Id = Guid.NewGuid(),
                            Title = "Introduction to Algorithms",
                            Subtitle = "A Comprehensive Guide",
                            ISBN = "978-0262033848",
                            Description = "A comprehensive guide to algorithms and data structures used in computer science.",
                            TableOfContents = "1. Foundations... 2. Sorting... 3. Data Structures...",
                            IntendedAudience = "Computer Science Students, Developers",
                            Price = 2999.99m,
                            StockQuantity = 100,
                            Format = ProductFormat.Print,
                            Type = ProductType.Book,
                            Status = ProductStatus.Published,
                            CategoryId = computerScienceCategory.Id,
                            CreatedAt = DateTime.UtcNow,
                            PublishedDate = DateTime.UtcNow.AddDays(-30)
                        },
                        new Product
                        {
                            Id = Guid.NewGuid(),
                            Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                            ISBN = "978-0132350884",
                            Description = "Learn how to write clean, maintainable code that any developer can understand and work with.",
                            IntendedAudience = "Software Developers, Architects",
                            Price = 2499.99m,
                            DiscountPrice = 1999.99m,
                            StockQuantity = 50,
                            Format = ProductFormat.Bundle,
                            Type = ProductType.Book,
                            Status = ProductStatus.Published,
                            CategoryId = computerScienceCategory.Id,
                            CreatedAt = DateTime.UtcNow,
                            PublishedDate = DateTime.UtcNow.AddDays(-15)
                        }
                    };

                    await _context.Products.AddRangeAsync(products);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Seeded {Count} sample products", products.Length);
                }
            }
        }
    }
}
