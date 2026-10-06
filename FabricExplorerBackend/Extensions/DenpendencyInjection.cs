using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Connection;
using FabricExplorerBackend.Features.Fabric.Client;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Features.Token;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Securities;
using FabricExplorerBackend.Mappers;
using FabricExplorerBackend.Mappers.FabricOperation;

namespace FabricExplorerBackend.Extensions
{
    public static class DenpendencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ITokenService, TokenService>();
            serviceCollection.AddScoped<IFabricContextFactory, FabricContextFactory>();
            serviceCollection.AddScoped<IConnectionService, ConnectionService>();
            serviceCollection.AddScoped<IClientFactory, ClientFactory>();
            serviceCollection.AddScoped<ICacheService, CacheService>();
            serviceCollection.AddScoped<ILakehouseService, LakehouseService>();
            serviceCollection.AddScoped<IFabricOperationService, FabricOperationService>();
            //serviceCollection.AddScoped<IFabricRestClient, FabricRestClient>();
            serviceCollection.AddScoped<IFabricRestClientFactory, FabricRestClientFactory>();
            serviceCollection.AddScoped<ILakehouseTableStatsService, LakehouseTableStatsService>();

            return serviceCollection;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISecretProtector, SecretProtector>();
            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            serviceCollection.AddScoped<IConnectionRepository, ConnectionRepository>();
            serviceCollection.AddScoped<IUserRepository, UserRepository>();
            serviceCollection.AddScoped<IFabricOperationRepository, FabricOperationRepository>();

            return serviceCollection;
        } 

        public static IServiceCollection AddMappers(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IConnectionMapper, ConnectionMapper>();
            serviceCollection.AddScoped<IFabricOperationMapper, FabricOperationMapper>();
            serviceCollection.AddScoped<ILakehouseMapper, LakehouseMapper>();

            return serviceCollection;
        }
    }
}
