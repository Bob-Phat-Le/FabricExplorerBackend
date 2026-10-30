using FabricExplorerBackend.Application.Catalog;
using FabricExplorerBackend.Application.Connection;
using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.MirroredDatabase;
using FabricExplorerBackend.Application.Notification;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Application.Workspace;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Infrastructures.BackgroundJobs;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Client;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.OneLake;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.QueryResults;
using FabricExplorerBackend.Infrastructures.Persistences;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Security;
using FabricExplorerBackend.Infrastructures.Sql;
using FabricExplorerBackend.Infrastructures.Workers.Processor;
using FabricExplorerBackend.Mappers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FabricExplorerBackend.Api.Startup
{
    public static class DenpendencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IFabricContextFactory, FabricContextFactory>();
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IClientFactory, ClientFactory>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<ILakehouseService, LakehouseService>();
            services.AddScoped<IFabricOperationService, FabricOperationService>();
            //services.AddScoped<IFabricRestClient, FabricRestClient>();
            services.AddScoped<IFabricRestClientFactory, FabricRestClientFactory>();
            services.AddScoped<ILakehouseTableStatsService, LakehouseTableStatsService>();
            services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<ISqlQueryService, SqlQueryService>();
            services.AddSingleton<ISingleFlight, SingleFlight>();
            services.AddScoped<ICachedFetcher, CachedFetcher>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IMirroredDatabaseService, MirroredDatabaseService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IEventStreamService, EventStreamService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<IDeltaTableApi, DeltaTableApi>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IQueryProcessor, QueryProcessor>();
            services.AddScoped<IQueryExecutor, FabricQueryExecutor>();

            // Query results: in-memory job queue + stateless Parquet reader/writer + the storage bridge.
            // Local filesystem today; replace LocalQueryResultStore with a blob implementation later, nothing else changes.
            services.AddSingleton<IQueryJobQueue, QueryJobQueue>();
            services.AddSingleton<IQueryResultStore, LocalQueryResultStore>();
            services.AddSingleton<IQueryResultWriter, ParquetResultWriter>();
            services.AddSingleton<IQueryResultReader, ParquetResultReader>();
            services.AddScoped<IQueryResultCleanupService, QueryResultCleanupService>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FabricExplorerDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("FabricExplorerDatabase")));
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisCache")!));

            services.AddSingleton<ISecretProtector, SecretProtector>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IConnectionRepository, ConnectionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFabricOperationRepository, FabricOperationRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IQueryMetadataRepository, QueryMetadataRepository>();

            // Event streaming (SSE): registry in-memory shared globally for the whole app so must be singleton
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IEventRegistry, EventRegistry>();
            services.AddSingleton<IEventPublisher, EventPublisher>();
            services.AddHostedService<EventStreamCleanupService>();

            // Background Worker
            services.AddScoped<IFabricOperationProcessor, FabricOperationProcessor>();
            services.AddHostedService<FabricOperationPollingService>();
            services.AddHostedService<QueryExecutionService>();
            services.AddHostedService<QueryResultCleanupWorker>();

            return services;
        }

        public static IServiceCollection AddApplicationMappers(this IServiceCollection services)
        {
            services.AddScoped<IConnectionMapper, ConnectionMapper>();
            services.AddScoped<IFabricOperationMapper, FabricOperationMapper>();
            services.AddScoped<ILakehouseMapper, LakehouseMapper>();
            services.AddScoped<IMirroredDatabaseMapper, MirroredDatabaseMapper>();
            services.AddScoped<INotificationMapper, NotificationMapper>();

            return services;
        }

        public static IServiceCollection AddApplicationDataProtection(this IServiceCollection services)
        {
            var environment = services
                .BuildServiceProvider()
                .GetRequiredService<IHostEnvironment>();

            services
                .AddDataProtection()
                .SetApplicationName("FabricExplorerBackend")
                .PersistKeysToFileSystem(
                    new DirectoryInfo(
                        Path.Combine(
                            environment.ContentRootPath,
                            "DataProtectionKeys")));

            services.AddSingleton<IDataProtector>(sp =>
                sp.GetRequiredService<IDataProtectionProvider>()
                  .CreateProtector("FabricExplorer.Secrets"));

            return services;
        }

        public static IServiceCollection AddApplicationCors(this IServiceCollection services)
        {
            // allow for localhost with any port
            services.AddCors(options =>
            {
                options.AddPolicy("MyAllowSpecificOrigin", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                            return false;

                        return uri.IsLoopback;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            return services;
        }

        public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ApiOptions>().BindConfiguration(ApiOptions.SectionName);
            services.AddOptions<AuthenticationOptions>().BindConfiguration(AuthenticationOptions.SectionName);
            services.AddOptions<AzureOptions>().BindConfiguration(AzureOptions.SectionName);
            services.AddOptions<CacheOptions>().BindConfiguration(CacheOptions.SectionName);
            services.AddOptions<ConnectionStringsOptions>().BindConfiguration(ConnectionStringsOptions.SectionName);
            services.AddOptions<EntraOptions>().BindConfiguration(EntraOptions.SectionName);
            services.AddOptions<FabricOptions>().BindConfiguration(FabricOptions.SectionName);
            services.AddOptions<OperationPollingOptions>().BindConfiguration(OperationPollingOptions.SectionName);
            services.AddOptions<EventStreamingOptions>().BindConfiguration(EventStreamingOptions.SectionName);
            services.AddOptions<CatalogOptions>().BindConfiguration(CatalogOptions.SectionName);
            services.AddOptions<QueryOptions>().BindConfiguration(QueryOptions.SectionName);
            services.AddOptions<QueryResultStorageOptions>().BindConfiguration(QueryResultStorageOptions.SectionName);

            return services;
        }
    }
}
