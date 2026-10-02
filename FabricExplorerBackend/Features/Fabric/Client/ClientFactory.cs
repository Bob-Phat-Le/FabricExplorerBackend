using Azure.Identity;
using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Features.Fabric.Client
{
    public class ClientFactory : IClientFactory
    {
        public FabricClient CreateClientAsync(ClientSecretCredential credential)
        {
            return new FabricClient(credential);
        }

        public FabricClient CreateClientAsync(string accessToken)
        {
            return new FabricClient(accessToken);
        }
    }
}
