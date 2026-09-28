using FabricExplorerBackend.Models.Responses;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IFabricContextFactory
    {
        Task<FabricContext> CreateFabricContextAsync(Guid tenantId, Guid connectionId, Guid workspaceId, string accessToken);
    }
}
