using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Repositories.Interfaces;
using FabricExplorerBackend.Services.Interfaces;

namespace FabricExplorerBackend.Services.Implements
{
    public class FabricContextFactory(
        IClientFactory clientFactory) : IFabricContextFactory
    {
        public async Task<FabricContext> CreateFabricContextAsync(Guid tenantId, Guid connectionId, Guid workspaceId, string accessToken)
        {
            return new FabricContext()
            {
                TenantId = tenantId,
                ConnectionId = connectionId,
                WorkspaceId = workspaceId,
                Client = clientFactory.CreateClientAsync(accessToken)
            };
        }
    }
}
