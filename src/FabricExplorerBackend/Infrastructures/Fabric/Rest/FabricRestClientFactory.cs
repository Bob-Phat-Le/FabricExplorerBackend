using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public class FabricRestClientFactory(
        IOptions<AzureOptions> azureOptions,
        IHttpClientFactory httpClientFactory,
        ITokenService tokenService) : IFabricRestClientFactory
    {
        public IFabricRestClient CreateFabricRestClient(
            Domain.Entities.Connection connection,
            FabricApiAudience audience = FabricApiAudience.Fabric)
        {
            var scopes = azureOptions.Value.Scopes;
            var scope = audience switch
            {
                FabricApiAudience.Storage => scopes.Storage,
                _ => scopes.Fabric
            };

            var httpClient = httpClientFactory.CreateClient("FabricClient");
            return new FabricRestClient(httpClient, tokenService, connection, scope);
        }
    }
}
