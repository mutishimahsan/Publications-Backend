using Application.Common;
using Application.Interfaces;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureDI(this IServiceCollection services, IConfiguration configuration)
        {
            // Database Context
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            // Distributed Cache (for cart storage)
            services.AddDistributedMemoryCache(); // For development
            // For production, use Redis:
            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration = configuration.GetConnectionString("Redis");
            //     options.InstanceName = "MENTISERA_";
            // });

            // Identity
            services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
            .AddSignInManager<SignInManager<User>>()
            .AddDefaultTokenProviders();

            // Configure Identity options
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            });

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            // JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");

            // Validate JWT configuration exists
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("JWT Secret is not configured. Please add 'JwtSettings:Secret' to appsettings.json");
            }

            if (secret.Length < 32)
            {
                throw new InvalidOperationException($"JWT Secret must be at least 32 characters long. Current length: {secret.Length}");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                // Support multiple ways to send the token
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // 1. Standard Authorization header
                        var token = context.Request.Headers["Authorization"].FirstOrDefault();

                        // 2. Custom header for Scalar compatibility
                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.Request.Headers["X-Access-Token"].FirstOrDefault();
                        }

                        // 3. Query string for easy testing
                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.Request.Query["access_token"].FirstOrDefault();
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            // Clean the token
                            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                token = token.Substring(7);
                            }

                            // Remove quotes
                            token = token.Trim('"');

                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(new
                        {
                            message = "Unauthorized: Invalid or expired token",
                            hint = "Use: Authorization: Bearer {token} OR X-Access-Token: {token}"
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            });

            // Authorization Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyConstants.RequireAdmin, policy =>
                    policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.ContentAdmin,
                                      RoleConstants.FinanceAdmin, RoleConstants.SupportAdmin));

                options.AddPolicy(PolicyConstants.RequireFinance, policy =>
                    policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.FinanceAdmin));

                options.AddPolicy(PolicyConstants.RequireContent, policy =>
                    policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.ContentAdmin));

                options.AddPolicy(PolicyConstants.RequireSupport, policy =>
                    policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.SupportAdmin));

                options.AddPolicy(PolicyConstants.RequireCustomer, policy =>
                    policy.RequireRole(RoleConstants.Customer));
            });

            // AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });

            // Unit of Work & Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Generic Repository (if needed separately)
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Specific Repositories (Register all repository interfaces with their implementations)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<IBlogRepository, BlogRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IBlogService, BlogService>();
            //services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();

            // Services
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<SeedService>();

            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<AuditInterceptor>();
            services.AddScoped<IAuditService, AuditService>();

            services.AddScoped<IDigitalAccessRepository, DigitalAccessRepository>();

            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // Audit Service (if implemented)
            // services.AddScoped<IAuditService, AuditService>();

            // Configure other settings
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // HttpContextAccessor (for FileStorageService, etc.)
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
