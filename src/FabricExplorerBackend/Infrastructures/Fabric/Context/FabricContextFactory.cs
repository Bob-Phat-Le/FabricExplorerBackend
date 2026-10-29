using FabricExplorerBackend.Configurations.Options;
using FabricExplorerBackend.Infrastructures.Fabric.Client;
using FabricExplorerBackend.Infrastructures.Identity;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Fabric.Context
{
    public class FabricContextFactory(
        ITokenService tokenService,
        IClientFactory clientFactory,
        IOptions<AzureOptions> azureOptions,
        IConfiguration configuration) : IFabricContextFactory
    {
        private AzureOptions azureOptions = azureOptions.Value;

        public async Task<FabricContext> CreateFabricContextWithTokenAsync(Domain.Entities.Connection connection, string accessToken)
        {
            return new FabricContext()
            {
                TenantId = connection.TenantId,
                ConnectionId = connection.Id,
                WorkspaceId = connection.WorkspaceId,
                Client = clientFactory.CreateClientAsync(accessToken)
            };
        }

        public async Task<FabricContext?> CreateFabricContextWithoutTokenAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken = default)
        {
            var scope = azureOptions.Scopes.Fabric;
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope!, cancellationToken);

            var context = await CreateFabricContextWithTokenAsync(connection, token);

            return context;
        }
    }
}
