using FabricExplorerBackend.Commons.Models.Responses;

namespace FabricExplorerBackend.Features.Fabric.Context
{
    public interface IFabricContextFactory
    {
        //Task<FabricContext> CreateFabricContextAsync(Guid tenantId, Guid connectionId, Guid workspaceId, string accessToken);
        Task<FabricContext?> CreateFabricContextAsync(Domain.Entities.Connection connection);
    }
}
