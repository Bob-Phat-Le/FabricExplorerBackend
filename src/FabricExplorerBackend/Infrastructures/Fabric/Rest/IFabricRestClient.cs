using FabricExplorerBackend.Commons.Fabric;

namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public interface IFabricRestClient
    {
        Task<FabricResponse<TResponse>?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
        Task<FabricResponse<TResponse>?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
        Task<FabricResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }
}
