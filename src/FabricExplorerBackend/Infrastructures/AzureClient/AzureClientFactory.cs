using Azure.Identity;
using Azure.Storage.Files.DataLake;

namespace FabricExplorerBackend.Infrastructures.AzureClient
{
    public class AzureClientFactory : IAzureClientFactory
    {
        public DataLakeServiceClient CreateClient(Uri uri, ClientSecretCredential credential)
        {
            return new DataLakeServiceClient(uri, credential);
        }
    }
}
