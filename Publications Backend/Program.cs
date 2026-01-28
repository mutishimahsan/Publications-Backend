
using Application.Services;
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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(); 
            }
            

            app.UseHttpsRedirection();

            app.UseRouting();

            // Use CORS
            app.UseCors("AllowAll");

            // Serve static files
            app.UseStaticFiles();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Webhook endpoints need raw body access
            app.MapPost("/api/webhook/stripe", async (HttpContext context) =>
            {
                using var scope = app.Services.CreateScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var signature = context.Request.Headers["Stripe-Signature"].ToString();

                var result = await paymentService.HandleStripeWebhookAsync(json, signature);

                if (result)
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("Webhook processed successfully");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Webhook processing failed");
                }
            });

            // Ensure database is created and migrations applied
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    await context.Database.MigrateAsync();

                    var seedService = services.GetRequiredService<SeedService>();
                    await seedService.SeedAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database migration / seeding failed");
                }
            }

            app.Run();
        }
    }
}
