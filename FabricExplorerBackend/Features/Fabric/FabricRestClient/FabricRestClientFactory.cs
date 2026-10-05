using FabricExplorerBackend.Features.Token;

namespace FabricExplorerBackend.Features.Fabric.FabricRestClient
{
    public class FabricRestClientFactory(
        IHttpClientFactory httpClientFactory,
        ITokenService tokenService) : IFabricRestClientFactory
    {
        public IFabricRestClient CreateFabricRestClient(Domain.Entities.Connection connection)
        {
            var httpClient = httpClientFactory.CreateClient("FabricClient");
            return new FabricRestClient(httpClient, tokenService, connection);
        }
    }
}
