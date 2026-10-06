namespace FabricExplorerBackend.Features.Fabric.FabricRestClient
{
    public interface IFabricRestClientFactory
    {
        IFabricRestClient CreateFabricRestClient(Domain.Entities.Connection connection);
    }
}
