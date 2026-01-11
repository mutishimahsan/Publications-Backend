using Application;
using Infrastructure;

namespace Publications_Backend
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAppDI(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationDI()
                .AddInfrastructureDI(configuration);
            return services;
        }
    }
}
