using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Features.Fabric.Operation;
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
        ILakehouseTableStatsService tableStatsService) : ILakehouseService
    {
    }
}
