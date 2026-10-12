namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public interface IFabricRestClientFactory
    {
        IFabricRestClient CreateFabricRestClient(Domain.Entities.Connection connection);
    }
}
