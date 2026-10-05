using FabricExplorerBackend.Features.Token;

namespace FabricExplorerBackend.Features.Fabric.FabricRestClient
{
    public class FabricRestClient(
        HttpClient httpClient,
        ITokenService tokenService,
        Domain.Entities.Connection connection) : IFabricRestClient
    {
        public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.GetAsync(endpoint, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.PatchAsJsonAsync(endpoint, request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.DeleteAsync(endpoint, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }
}
