using Azure.Identity;
using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IClientFactory
    {
        FabricClient CreateClientAsync(string accessToken);
        FabricClient CreateClientAsync(ClientSecretCredential credential);
    }
}
