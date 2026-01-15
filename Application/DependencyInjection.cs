using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDI(this IServiceCollection services)
        {
            // AutoMapper - Method 1: Scan current assembly
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });

            // OR Method 2: Specify the profile class
            // services.AddAutoMapper(typeof(AutoMapperProfile));

            // Application Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}