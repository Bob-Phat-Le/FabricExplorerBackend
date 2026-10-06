using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Features.Fabric.Client;
using FabricExplorerBackend.Features.Token;
using FabricExplorerBackend.Infrastructures.Securities;

namespace FabricExplorerBackend.Features.Fabric.Context
{
    public class FabricContextFactory(
        ITokenService tokenService,
        IClientFactory clientFactory,
        ISecretProtector secretProtector,
        IConfiguration configuration) : IFabricContextFactory
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

        public async Task<FabricContext?> CreateFabricContextAsync(Domain.Entities.Connection connection)
        {
            var scope = configuration.GetValue<string>("Scopes:api.fabric");
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope!);

            var context = await CreateFabricContextAsync(connection.TenantId, connection.Id, connection.WorkspaceId, token);

            return context;
        }
    }
}
