namespace FabricExplorerBackend.Infrastructures.Fabric.Context
{
    public interface IFabricContextFactory
    {
        Task<FabricContext> CreateFabricContextWithTokenAsync(Domain.Entities.Connection connection, string accessToken);
        Task<FabricContext?> CreateFabricContextWithoutTokenAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken = default);
    }
}
