
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Publications_Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddAppDI(builder.Configuration);

            // Configure Kestrel for production
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxRequestBodySize = 52428800; // 50MB for file uploads
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();

                // Apply migrations and seed database in development
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Apply pending migrations
                    await context.Database.MigrateAsync();

                    // Seed database
                    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
                    await seedService.SeedAsync();
                }
            }
            else
            {
                // For production, apply migrations on startup
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Apply pending migrations
                    await context.Database.MigrateAsync();

                    // Seed only if database is empty (first startup)
                    if (!await context.Users.AnyAsync())
                    {
                        var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
                        await seedService.SeedAsync();
                    }
                }
            }

            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors("AllowAll");

            // Serve static files
            app.UseStaticFiles();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Ensure database is created and migrations applied
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    context.Database.Migrate(); // Apply migrations automatically
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            app.Run();
        }
    }
}
