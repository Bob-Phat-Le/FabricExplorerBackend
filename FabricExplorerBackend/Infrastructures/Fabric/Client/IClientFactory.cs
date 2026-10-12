using Azure.Identity;
using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Infrastructures.Fabric.Client
{
    public interface IClientFactory
    {
        FabricClient CreateClientAsync(string accessToken);
        FabricClient CreateClientAsync(ClientSecretCredential credential);
    }
}
