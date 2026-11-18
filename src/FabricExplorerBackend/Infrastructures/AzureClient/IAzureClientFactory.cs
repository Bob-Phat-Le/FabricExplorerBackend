using Azure.Identity;
using Azure.Storage.Files.DataLake;

namespace FabricExplorerBackend.Infrastructures.AzureClient
{
    public interface IAzureClientFactory
    {
        DataLakeServiceClient CreateClient(Uri uri, ClientSecretCredential credential);
    }
}
