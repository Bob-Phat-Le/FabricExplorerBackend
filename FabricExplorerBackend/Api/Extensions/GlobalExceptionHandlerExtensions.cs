using FabricExplorerBackend.Api.ExceptionHandling;

namespace FabricExplorerBackend.Api.Extensions
{
    public static class GlobalExceptionHandlerExtensions
    {
        public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();

            services.AddProblemDetails();
            return services;
        }
    }
}
