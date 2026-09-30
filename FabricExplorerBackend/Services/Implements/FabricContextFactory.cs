using FabricExplorerBackend.Entities;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Repositories.Interfaces;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Services.Interfaces;

namespace FabricExplorerBackend.Services.Implements
{
    public class FabricContextFactory(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IClientFactory clientFactory,
        ISecretProtector secretProtector) : IFabricContextFactory
    {
        private async Task<FabricContext> CreateFabricContextAsync(Guid tenantId, Guid connectionId, Guid workspaceId, string accessToken)
        {
            return new FabricContext()
            {
                TenantId = tenantId,
                ConnectionId = connectionId,
                WorkspaceId = workspaceId,
                Client = clientFactory.CreateClientAsync(accessToken)
            };
        }

        public async Task<FabricContext?> CreateFabricContextAsync(Connection connection)
        {
            var token = await tokenService.GetTokenAsync(connection.TenantId, connection.ClientId, connection.WorkspaceId);
            if (string.IsNullOrEmpty(token))
                token = await tokenService.CreateTokenAsync(
                    connection.TenantId, 
                    connection.ClientId, 
                    connection.WorkspaceId, 
                    secretProtector.Unprotect(connection.ClientSecret));
                
            var context = await CreateFabricContextAsync(connection.TenantId, connection.Id, connection.WorkspaceId, token);

            return context;
        }
    }
}
