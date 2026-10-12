using FabricExplorerBackend.Application.Connection;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.MirroredDatabase;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Application.Workspace;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Client;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Security;
using FabricExplorerBackend.Infrastructures.Sql;
using FabricExplorerBackend.Mappers;

namespace FabricExplorerBackend.Api.Startup
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
            serviceCollection.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            serviceCollection.AddScoped<ISqlQueryService, SqlQueryService>();
            serviceCollection.AddSingleton<ISingleFlight, SingleFlight>();   // danh sách lần chạy đang diễn ra phải dùng chung toàn app
            serviceCollection.AddScoped<ICachedFetcher, CachedFetcher>();
            serviceCollection.AddScoped<IWorkspaceService, WorkspaceService>();
            serviceCollection.AddScoped<IMirroredDatabaseService, MirroredDatabaseService>();
            serviceCollection.AddScoped<IWarehouseService, WarehouseService>();

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
            serviceCollection.AddScoped<IMirroredDatabaseMapper, MirroredDatabaseMapper>();

            return serviceCollection;
        }
    }
}
