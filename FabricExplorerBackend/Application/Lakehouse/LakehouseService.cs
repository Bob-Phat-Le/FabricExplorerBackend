using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IFabricRestClientFactory fabricRestClientFactory,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService,
        ILakehouseMapper lakehouseMapper,
        ILakehouseTableStatsService tableStatsService,
        ICachedFetcher cachedFetcher) : ILakehouseService
    {
    }
}
