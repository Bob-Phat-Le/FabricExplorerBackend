namespace FabricExplorerBackend.Api.Startup
{
    public static class Startup
    {
        public static IServiceCollection AddStartupDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationDataProtection();
            services.AddApplicationMappers();
            services.AddApplicationServices();
            services.AddInfrastructureServices(configuration);
            services.AddApplicationCors();
            services.AddApplicationOptions(configuration);

            return services;
        }
    }
}
