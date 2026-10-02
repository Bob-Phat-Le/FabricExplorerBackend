using Azure.Identity;
using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Features.Fabric.Client
{
    public interface IClientFactory
    {
        FabricClient CreateClientAsync(string accessToken);
        FabricClient CreateClientAsync(ClientSecretCredential credential);
    }
}
