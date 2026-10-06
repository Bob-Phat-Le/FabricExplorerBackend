using FabricModel = Microsoft.Fabric.Api.Warehouse.Models;
using FabricExplorerBackendModel = FabricExplorerBackend.Commons;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Warehouse;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    public class WarehouseService(
        IFabricContextFactory fabricContextFactory,
        IUnitOfWork unitOfWork) : IWarehouseService
    {
        public async Task<FabricExplorerBackendModel.Result<IEnumerable<WarehouseResponse>>> GetAllWarehouseByWorkspaceIdAsync(Guid workspaceId)
        {
            try
            {
                var context = fabricContextFactory.CreateFabricContextAsync()
            }
        }
    }
}
