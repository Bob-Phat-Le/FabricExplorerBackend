using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Features.Fabric.Client;
using FabricExplorerBackend.Features.Token;

namespace FabricExplorerBackend.Features.Fabric.Context
{
    public class FabricContextFactory(
        ITokenService tokenService,
        IClientFactory clientFactory,
        IConfiguration configuration) : IFabricContextFactory
    {
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
            var scope = configuration.GetValue<string>("Scopes:api.fabric");
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope!, cancellationToken);

            var context = await CreateFabricContextWithTokenAsync(connection, token);

            return context;
        }
    }
}
