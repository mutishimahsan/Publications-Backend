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
            const string adminEmail = "admin@mentisera.pk";
            const string adminPassword = "Admin@123";
            const string adminRole = "SuperAdmin";
            const string systemUser = "SYSTEM";

            _logger.LogInformation("🔹 Seeding admin user...");

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(adminRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(adminRole));

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                        _logger.LogError("❌ Role creation failed: {Desc}", error.Description);

                    return;
                }
            }

            // Ignore global filters (IsDeleted, etc.)
            var adminUser = await _userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == adminEmail.ToUpper());

            if (adminUser != null)
            {
                _logger.LogInformation("✅ Admin user already exists");

                if (!await _userManager.IsInRoleAsync(adminUser, adminRole))
                    await _userManager.AddToRoleAsync(adminUser, adminRole);

                adminUser.IsActive = true;
                adminUser.EmailConfirmed = true;
                adminUser.UpdatedAt = DateTime.UtcNow;
                adminUser.UpdatedBy = systemUser;

                await _userManager.UpdateAsync(adminUser);
                return;
            }

            // Create admin user
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpper(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpper(),
                FullName = "System Administrator",
                UserType = UserType.SuperAdmin,
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumber = "+923001234567",

                CreatedAt = DateTime.UtcNow,
                CreatedBy = systemUser,      // ✅ FIXED
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = systemUser,      // ✅ FIXED

                SecurityStamp = Guid.NewGuid().ToString()
            };

            var createResult = await _userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    _logger.LogError("❌ Admin user creation failed: {Desc}", error.Description);

                return;
            }

            await _userManager.AddToRoleAsync(adminUser, adminRole);

            _logger.LogInformation("🎉 Admin user seeded successfully ({Email})", adminEmail);
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
