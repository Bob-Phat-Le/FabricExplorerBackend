using FabricExplorerBackend.Infrastructures.Identity;

namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public class FabricRestClientFactory(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ITokenService tokenService) : IFabricRestClientFactory
    {
        public IFabricRestClient CreateFabricRestClient(Domain.Entities.Connection connection)
        {
            var httpClient = httpClientFactory.CreateClient("FabricClient");
            return new FabricRestClient(httpClient, configuration, tokenService, connection);
        }
    }
}
