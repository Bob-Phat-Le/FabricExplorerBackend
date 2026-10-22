using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService(
        ILogger<LakehouseService> logger,
        IUnitOfWork unitOfWork,
        IOptions<FabricOptions> fabricOptions,
        IOptions<CacheOptions> cacheOptions,
        IFabricRestClientFactory fabricRestClientFactory,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService,
        ILakehouseMapper lakehouseMapper,
        ILakehouseTableStatsService tableStatsService,
        IEventStreamService eventStreamService,
        ICachedFetcher cachedFetcher) : ILakehouseService
    {
        private FabricOptions fabricOptions = fabricOptions.Value;
        private CacheOptions cacheOption = cacheOptions.Value;
    }
}
