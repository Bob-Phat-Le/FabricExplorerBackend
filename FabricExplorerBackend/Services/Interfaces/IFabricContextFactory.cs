using FabricExplorerBackend.Entities;
using FabricExplorerBackend.Models.Responses;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IFabricContextFactory
    {
        //Task<FabricContext> CreateFabricContextAsync(Guid tenantId, Guid connectionId, Guid workspaceId, string accessToken);
        Task<FabricContext?> CreateFabricContextAsync(Connection connection);
    }
}
