using Azure.Core;

namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public interface IFabricRestClient
    {
        Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
        Task PutAsync(string endpoint, CancellationToken cancellationToken = default);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
        Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }
}
