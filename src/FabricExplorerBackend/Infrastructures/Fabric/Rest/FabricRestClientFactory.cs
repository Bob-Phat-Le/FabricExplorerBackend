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
        public IFabricRestClient CreateFabricRestClient(Domain.Entities.Connection connection)
        {
            var httpClient = httpClientFactory.CreateClient("FabricClient");
            return new FabricRestClient(httpClient, azureOptions, tokenService, connection);
        }
    }
}
