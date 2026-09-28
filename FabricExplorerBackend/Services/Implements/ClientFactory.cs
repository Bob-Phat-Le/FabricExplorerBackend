using Azure.Identity;
using FabricExplorerBackend.Services.Interfaces;
using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Services.Implements
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
